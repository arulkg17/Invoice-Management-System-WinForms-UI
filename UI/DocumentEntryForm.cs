using System.ComponentModel;
using System.Text.Json;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// What differs between Purchase Order and Sales Invoice entry (they share one form).
public sealed record DocumentConfig(
    string Controller, string Title, string PartyLabel, bool PartyIsVendor,
    string PartyIdField, string DateField, bool HasDueDate = false);

public static class DocumentConfigs
{
    public static readonly DocumentConfig PurchaseOrder =
        new("PurchaseOrder", "Purchase Order", "Vendor", true, "vendorId", "poDate");
    public static readonly DocumentConfig SalesInvoice =
        new("SalesInvoice", "Sales Invoice", "Customer", false, "customerId", "invoiceDate", true);
}

public sealed record PartyOption(int Id, string Name);

/// One line of a Purchase Order / Sales Invoice. Tax and totals are calculated here and again by the server.
public sealed class DocLine
{
    public int ItemId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal Taxable => Quantity * Rate - DiscountAmount;
    public decimal TaxAmount => Math.Round(Taxable * TaxPercent / 100m, 2);
    public decimal Amount => Taxable + TaxAmount;
}

/// New / Edit form for Purchase Order and Sales Invoice: header + editable lines.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class DocumentEntryForm : Form
{
    private readonly ApiClient _api;
    private readonly DocumentConfig _cfg;
    private readonly int? _editId;
    private List<Itemmaster> _items = new();
    private readonly BindingList<DocLine> _lines = new();

    private readonly ComboBox _party = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Width = 320, DisplayMember = "Name", ValueMember = "Id" };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Width = 150 };
    private readonly DateTimePicker _due = new() { Format = DateTimePickerFormat.Short, Width = 150, ShowCheckBox = true, Checked = false };
    private readonly InputBox _notes = new() { Width = 520 };
    private readonly DataGridView _grid = new();
    private readonly Label _totals = new() { AutoSize = true, Font = Theme.Bold, ForeColor = Theme.TextPrimary, Margin = new Padding(16, 10, 0, 0) };
    private readonly Label _error = new() { Dock = DockStyle.Top, Height = 26, ForeColor = Theme.Danger, Padding = new Padding(16, 0, 0, 0) };

    public DocumentEntryForm(ApiClient api, DocumentConfig cfg, int? editId = null)
    {
        _api = api; _cfg = cfg; _editId = editId;

        Text = (editId is null ? "New " : "Edit ") + cfg.Title;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Body;
        BackColor = Theme.AppBg;
        Size = new Size(1000, 680);
        MinimumSize = new Size(860, 520);
        StartPosition = FormStartPosition.CenterParent;

        // ---- header ----
        var header = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16, 14, 16, 0), BackColor = Color.Transparent };
        header.Controls.Add(Field(cfg.PartyLabel, _party));
        header.Controls.Add(Field("Date", _date));
        if (cfg.HasDueDate) header.Controls.Add(Field("Due date", _due));
        header.Controls.Add(Field("Notes", _notes));

        // ---- lines grid ----
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        Theme.StyleGrid(_grid);
        _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Item", HeaderText = "Item", DataPropertyName = nameof(DocLine.ItemId), DisplayMember = "ItemName", ValueMember = "Id", FlatStyle = FlatStyle.Flat, FillWeight = 220 });
        _grid.Columns.Add(Num("Quantity", nameof(DocLine.Quantity), false));
        _grid.Columns.Add(Num("Rate", nameof(DocLine.Rate), false));
        _grid.Columns.Add(Num("Discount", nameof(DocLine.DiscountAmount), false));
        _grid.Columns.Add(Num("Tax %", nameof(DocLine.TaxPercent), false));
        _grid.Columns.Add(Num("Line total", nameof(DocLine.Amount), true));
        _grid.Columns.Add(new DataGridViewButtonColumn { Name = "Remove", HeaderText = "", Text = "Remove", UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 100 });
        _grid.DataSource = _lines;

        _grid.CurrentCellDirtyStateChanged += (_, _) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _grid.CellValueChanged += OnCellValueChanged;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Remove")
            {
                _grid.EndEdit();
                _lines.RemoveAt(e.RowIndex);
                Recalc();
            }
        };

        var gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(6, 6, 6, 11) };
        gridCard.Controls.Add(_grid);
        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 0), BackColor = Color.Transparent };
        gridHost.Controls.Add(gridCard);

        // ---- footer ----
        var left = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, BackColor = Color.Transparent };
        left.Controls.Add(Ui.Button("＋ Add line", (_, _) => { _lines.Add(new DocLine()); Recalc(); }, ButtonKind.Ghost));
        left.Controls.Add(_totals);
        var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, BackColor = Color.Transparent };
        right.Controls.Add(Ui.Button("Save", async (_, _) => await SaveAsync()));
        right.Controls.Add(Ui.Button("Cancel", (_, _) => DialogResult = DialogResult.Cancel, ButtonKind.Ghost));
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(16, 12, 16, 8), BackColor = Color.Transparent };
        footer.Controls.Add(right);
        footer.Controls.Add(left);

        // z-order: Fill first, then docked edges
        Controls.Add(gridHost);
        Controls.Add(footer);
        Controls.Add(_error);
        Controls.Add(header);

        if (editId is null) _lines.Add(new DocLine());
        Recalc();
        Shown += async (_, _) => await Ui.RunAsync(LoadAsync);
    }

    static Control Field(string label, Control c)
    {
        var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 18, 6) };
        p.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.Muted });
        p.Controls.Add(c);
        return p;
    }

    static DataGridViewTextBoxColumn Num(string header, string prop, bool readOnly)
    {
        var col = new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = prop, ReadOnly = readOnly, FillWeight = 80 };
        col.DefaultCellStyle.Format = "N2";
        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return col;
    }

    async Task LoadAsync()
    {
        List<PartyOption> parties = _cfg.PartyIsVendor
            ? (await new MasterService<Vendor>(_api, "Vendor").GetAllAsync()).Select(v => new PartyOption(v.Id, v.VendorName)).ToList()
            : (await new MasterService<Customer>(_api, "Customer").GetAllAsync()).Select(c => new PartyOption(c.Id, c.CustomerName)).ToList();
        _items = await new MasterService<Itemmaster>(_api, "Itemmaster").GetAllAsync();

        _party.DataSource = parties;
        _party.SelectedIndex = -1;
        ((DataGridViewComboBoxColumn)_grid.Columns["Item"]!).DataSource = _items;

        if (_editId is int id) await LoadExistingAsync(id);
    }

    // ---- edit mode: GET api/v1/{controller}/GetById/{id} ----
    async Task LoadExistingAsync(int id)
    {
        var doc = await _api.RequestAsync<JsonElement>(HttpMethod.Get, $"api/v1/{_cfg.Controller}/GetById/{id}");
        if (doc.ValueKind != JsonValueKind.Object) { _error.Text = "Could not load the document."; return; }

        _party.SelectedValue = JsonHelp.GetInt(doc, _cfg.PartyIdField);
        _date.Value = JsonHelp.GetDate(doc, _cfg.DateField) ?? DateTime.Today;
        if (_cfg.HasDueDate)
        {
            var due = JsonHelp.GetDate(doc, "dueDate");
            _due.Checked = due is not null;
            if (due is DateTime d) _due.Value = d;
        }
        _notes.Text = JsonHelp.Find(doc, "notes")?.ToString() ?? "";

        _lines.Clear();
        if (JsonHelp.Find(doc, "details") is { ValueKind: JsonValueKind.Array } arr)
            foreach (var d in arr.EnumerateArray())
                _lines.Add(new DocLine
                {
                    ItemId = JsonHelp.GetInt(d, "itemmasterId"),
                    Quantity = JsonHelp.GetDec(d, "quantity"),
                    Rate = JsonHelp.GetDec(d, "rate"),
                    DiscountAmount = JsonHelp.GetDec(d, "discountAmount"),
                    TaxPercent = JsonHelp.GetDec(d, "taxPercent"),
                });
        if (_lines.Count == 0) _lines.Add(new DocLine());
        Recalc();
    }

    void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        // default the rate from the item master when an item is picked
        if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Item" && _grid.Rows[e.RowIndex].DataBoundItem is DocLine l && l.Rate == 0)
        {
            var item = _items.FirstOrDefault(i => i.Id == l.ItemId);
            if (item is not null) l.Rate = item.Rate ?? 0;
        }
        Recalc();
        if (e.RowIndex >= 0) _grid.InvalidateRow(e.RowIndex);
    }

    void Recalc() =>
        _totals.Text = $"Sub total {_lines.Sum(l => l.Taxable):N2}     Tax {_lines.Sum(l => l.TaxAmount):N2}     Total {_lines.Sum(l => l.Amount):N2}";

    async Task SaveAsync()
    {
        _grid.EndEdit();
        _error.Text = "";
        var lines = _lines.Where(l => l.ItemId > 0 && l.Quantity > 0).ToList();
        if (_party.SelectedValue is not int partyId) { _error.Text = $"Select a {_cfg.PartyLabel.ToLower()}."; return; }
        if (lines.Count == 0) { _error.Text = "Add at least one line with an item and quantity."; return; }

        // property names = your PurchaseOrderDto / SalesInvoiceDto (camelCase, matched case-insensitively)
        var body = new Dictionary<string, object?>
        {
            [_cfg.PartyIdField] = partyId,
            [_cfg.DateField] = _date.Value.Date,
            ["notes"] = _notes.Text,
            ["subTotal"] = lines.Sum(l => l.Taxable),
            ["taxAmount"] = lines.Sum(l => l.TaxAmount),
            ["totalAmount"] = lines.Sum(l => l.Amount),
            ["details"] = lines.Select(l => new
            {
                itemmasterId = l.ItemId, quantity = l.Quantity, rate = l.Rate,
                discountAmount = l.DiscountAmount, taxPercent = l.TaxPercent,
                taxAmount = l.TaxAmount, lineTotal = l.Amount,
            }).ToList(),
        };
        if (_cfg.HasDueDate) body["dueDate"] = _due.Checked ? _due.Value.Date : null;

        await Ui.RunAsync(async () =>
        {
            if (_editId is int id)
                await _api.RequestAsync<object>(HttpMethod.Put, $"api/v1/{_cfg.Controller}/Update/{id}", body);
            else
                await _api.RequestAsync<object>(HttpMethod.Post, $"api/v1/{_cfg.Controller}/Create", body);
            DialogResult = DialogResult.OK;
        });
    }
}
