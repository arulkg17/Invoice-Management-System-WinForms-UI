using System.ComponentModel;
using System.Text.Json;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

public sealed record PoOption(int Id, string Number);

/// One purchase-order line being received. Rate, discount and tax come from the PO on the server.
public sealed class ReceiptLine
{
    [Browsable(false)] public int PurchaseOrderDetailId { get; set; }
    public string ItemName { get; set; } = "";
    public decimal Ordered { get; set; }
    public decimal AlreadyReceived { get; set; }
    public decimal Pending => Ordered - AlreadyReceived;
    public decimal ReceiveNow { get; set; }
}

/// Receipt (GRN) entry: pick a Purchase Order, then enter how much of each line arrived.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class ReceiptEntryForm : Form
{
    private readonly ApiClient _api;
    private readonly int? _editId;
    private List<Itemmaster> _items = new();
    private readonly BindingList<ReceiptLine> _lines = new();
    private bool _suppress;

    private readonly ComboBox _po = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Width = 320, DisplayMember = "Number", ValueMember = "Id" };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Width = 150 };
    private readonly InputBox _notes = new() { Width = 520 };
    private readonly DataGridView _grid = new();
    private readonly Label _error = new() { Dock = DockStyle.Top, Height = 26, ForeColor = Theme.Danger, Padding = new Padding(16, 0, 0, 0) };

    public ReceiptEntryForm(ApiClient api, int? editId = null)
    {
        _api = api; _editId = editId;

        Text = editId is null ? "New Receipt" : "Edit Receipt";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Body;
        BackColor = Theme.AppBg;
        Size = new Size(940, 620);
        MinimumSize = new Size(800, 480);
        StartPosition = FormStartPosition.CenterParent;
        _po.Enabled = editId is null;                  // an existing receipt keeps its purchase order

        var header = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16, 14, 16, 0), BackColor = Color.Transparent };
        header.Controls.Add(Field("Purchase order", _po));
        header.Controls.Add(Field("Receipt date", _date));
        header.Controls.Add(Field("Notes", _notes));

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Theme.StyleGrid(_grid);
        _grid.Columns.Add(Col("Item", nameof(ReceiptLine.ItemName), true, 240));
        _grid.Columns.Add(Col("Ordered", nameof(ReceiptLine.Ordered), true, 90));
        _grid.Columns.Add(Col("Received so far", nameof(ReceiptLine.AlreadyReceived), true, 110));
        _grid.Columns.Add(Col("Pending", nameof(ReceiptLine.Pending), true, 90));
        _grid.Columns.Add(Col("Receive now", nameof(ReceiptLine.ReceiveNow), false, 110));
        _grid.DataSource = _lines;
        _grid.DataError += (_, e) => e.ThrowException = false;

        var gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(6, 6, 6, 11) };
        gridCard.Controls.Add(_grid);
        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 0), BackColor = Color.Transparent };
        gridHost.Controls.Add(gridCard);

        var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, BackColor = Color.Transparent };
        right.Controls.Add(Ui.Button("Save", async (_, _) => await SaveAsync()));
        right.Controls.Add(Ui.Button("Cancel", (_, _) => DialogResult = DialogResult.Cancel, ButtonKind.Ghost));
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(16, 12, 16, 8), BackColor = Color.Transparent };
        footer.Controls.Add(right);

        Controls.Add(gridHost);
        Controls.Add(footer);
        Controls.Add(_error);
        Controls.Add(header);

        _po.SelectionChangeCommitted += async (_, _) =>
        {
            if (_suppress || _po.SelectedValue is not int poId) return;
            await Ui.RunAsync(() => FetchLinesAsync(poId, null));
        };
        Shown += async (_, _) => await Ui.RunAsync(LoadAsync);
    }

    static Control Field(string label, Control c)
    {
        var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 18, 6) };
        p.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.Muted });
        p.Controls.Add(c);
        return p;
    }

    static DataGridViewTextBoxColumn Col(string header, string prop, bool readOnly, float weight) => new()
    {
        HeaderText = header, DataPropertyName = prop, ReadOnly = readOnly, FillWeight = weight,
    };

    async Task FetchLinesAsync(int poId, Dictionary<int, decimal>? existing)
    {
        var po = await _api.RequestAsync<JsonElement>(HttpMethod.Get, $"api/v1/PurchaseOrder/GetById/{poId}");
        _lines.Clear();
        if (po.ValueKind != JsonValueKind.Object) { _error.Text = "Could not load the purchase order."; return; }

        if (JsonHelp.Find(po, "details") is { ValueKind: JsonValueKind.Array } arr)
            foreach (var d in arr.EnumerateArray())
            {
                var detailId = JsonHelp.GetInt(d, "id");
                var itemId = JsonHelp.GetInt(d, "itemmasterId");
                var line = new ReceiptLine
                {
                    PurchaseOrderDetailId = detailId,
                    ItemName = _items.FirstOrDefault(i => i.Id == itemId)?.ItemName ?? $"Item {itemId}",
                    Ordered = JsonHelp.GetDec(d, "quantity"),
                    AlreadyReceived = JsonHelp.GetDec(d, "receivedQuantity"),
                };
                // new receipt: default to "receive everything still pending"; edit: the saved quantity
                line.ReceiveNow = existing is null ? Math.Max(line.Pending, 0) : existing.GetValueOrDefault(detailId);
                _lines.Add(line);
            }
    }

    async Task LoadAsync()
    {
        _items = await new MasterService<Itemmaster>(_api, "Itemmaster").GetAllAsync();
        var pos = await _api.RequestAsync<List<PurchaseOrderRow>>(HttpMethod.Get, "api/v1/PurchaseOrder/GetAll") ?? new();

        // a new receipt can only be raised against an order that is no longer a draft and not cancelled
        var usable = _editId is null
            ? pos.Where(p => !string.Equals(p.Status, "Draft", StringComparison.OrdinalIgnoreCase)
                          && !string.Equals(p.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            : pos;
        _suppress = true;
        _po.DataSource = usable.Select(p => new PoOption(p.Id, p.PONumber ?? $"PO #{p.Id}")).ToList();
        _po.SelectedIndex = -1;
        _suppress = false;

        if (_editId is int id)
        {
            var rc = await _api.RequestAsync<JsonElement>(HttpMethod.Get, $"api/v1/Receipt/GetById/{id}");
            if (rc.ValueKind != JsonValueKind.Object) { _error.Text = "Could not load the receipt."; return; }

            var poId = JsonHelp.GetInt(rc, "purchaseOrderId");
            _date.Value = JsonHelp.GetDate(rc, "receiptDate") ?? DateTime.Today;
            _notes.Text = JsonHelp.Find(rc, "notes")?.ToString() ?? "";

            var existing = new Dictionary<int, decimal>();
            if (JsonHelp.Find(rc, "details") is { ValueKind: JsonValueKind.Array } arr)
                foreach (var d in arr.EnumerateArray())
                    existing[JsonHelp.GetInt(d, "purchaseOrderDetailId")] = JsonHelp.GetDec(d, "receivedQuantity");

            _suppress = true; _po.SelectedValue = poId; _suppress = false;
            await FetchLinesAsync(poId, existing);
        }
    }

    async Task SaveAsync()
    {
        _grid.EndEdit();
        _error.Text = "";
        if (_po.SelectedValue is not int poId) { _error.Text = "Select a purchase order."; return; }
        var lines = _lines.Where(l => l.ReceiveNow > 0).ToList();
        if (lines.Count == 0) { _error.Text = "Enter a received quantity for at least one line."; return; }
        var over = lines.FirstOrDefault(l => l.ReceiveNow > l.Pending);
        if (over is not null) { _error.Text = $"{over.ItemName}: receiving more than the pending quantity ({over.Pending:0.##})."; return; }

        // property names = your ReceiptDto / ReceiptDetailDto
        var body = new
        {
            receiptDate = _date.Value.Date,
            purchaseOrderId = poId,
            notes = _notes.Text,
            details = lines.Select(l => new { purchaseOrderDetailId = l.PurchaseOrderDetailId, receivedQuantity = l.ReceiveNow }).ToList(),
        };

        await Ui.RunAsync(async () =>
        {
            if (_editId is int id)
                await _api.RequestAsync<object>(HttpMethod.Put, $"api/v1/Receipt/Update/{id}", body);
            else
                await _api.RequestAsync<object>(HttpMethod.Post, "api/v1/Receipt/Create", body);
            DialogResult = DialogResult.OK;
        });
    }
}
