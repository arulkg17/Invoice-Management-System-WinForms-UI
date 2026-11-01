using System.ComponentModel;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// New Purchase Order: header (vendor, date, remarks) + editable lines grid.
/// Receipt and Sales Invoice entry forms follow exactly the same pattern.
public sealed class PurchaseOrderEntryForm : Form
{
    public sealed class Line
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal Rate { get; set; }
        public decimal Amount => Quantity * Rate;
    }

    private readonly ApiClient _api;
    private List<Itemmaster> _items = new();
    private readonly BindingList<Line> _lines = new();

    private readonly ComboBox _vendor = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Width = 140 };
    private readonly TextBox _remarks = new() { Width = 300 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = true, RowHeadersVisible = false, BackgroundColor = Color.White, AutoGenerateColumns = false };
    private readonly Label _total = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), Margin = new Padding(20, 6, 0, 0) };

    public PurchaseOrderEntryForm(ApiClient api)
    {
        _api = api;
        Text = "New Purchase Order";
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;

        var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(12, 10, 0, 0) };
        header.Controls.Add(new Label { Text = "Vendor", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        header.Controls.Add(_vendor);
        header.Controls.Add(new Label { Text = "Date", AutoSize = true, Margin = new Padding(16, 6, 4, 0) });
        header.Controls.Add(_date);
        header.SetFlowBreak(_date, true);
        header.Controls.Add(new Label { Text = "Remarks", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        header.Controls.Add(_remarks);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12, 8, 0, 0) };
        footer.Controls.Add(Ui.Button("Save", async (_, _) => await SaveAsync()));
        footer.Controls.Add(Ui.Button("Cancel", (_, _) => DialogResult = DialogResult.Cancel));
        footer.Controls.Add(_total);

        _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Item", HeaderText = "Item", DataPropertyName = nameof(Line.ItemId), DisplayMember = "ItemName", ValueMember = "ItemmasterId", Width = 300 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = nameof(Line.Quantity), Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rate", DataPropertyName = nameof(Line.Rate), Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Amount", DataPropertyName = nameof(Line.Amount), ReadOnly = true, Width = 120 });
        _grid.DataSource = _lines;

        // commit combo selections immediately, recalc totals on any edit
        _grid.CurrentCellDirtyStateChanged += (_, _) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _grid.CellValueChanged += OnCellValueChanged;
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.UserDeletedRow += (_, _) => Recalc();

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(header);
        Recalc();

        Shown += async (_, _) => await Ui.RunAsync(LoadLookupsAsync);
    }

    async Task LoadLookupsAsync()
    {
        var vendors = await new MasterService<Vendor>(_api, "Vendor").GetAllAsync();
        _items = await new MasterService<Itemmaster>(_api, "Itemmaster").GetAllAsync();

        _vendor.DisplayMember = nameof(Vendor.VendorName);
        _vendor.ValueMember = nameof(Vendor.Id);
        _vendor.DataSource = vendors;
        ((DataGridViewComboBoxColumn)_grid.Columns["Item"]!).DataSource = _items;
    }

    void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == 0 && _grid.Rows[e.RowIndex].DataBoundItem is Line l && l.Rate == 0)
        {
            var item = _items.FirstOrDefault(i => i.Id == l.ItemId);
            if (item is not null) l.Rate = item.SalePrice;      // default the rate from the item master
        }
        Recalc();
        if (e.RowIndex >= 0) _grid.InvalidateRow(e.RowIndex);
    }

    void Recalc() => _total.Text = $"Total: {_lines.Sum(l => l.Amount):N2}";

    async Task SaveAsync()
    {
        var lines = _lines.Where(l => l.ItemId > 0 && l.Quantity > 0).ToList();
        if (_vendor.SelectedValue is not int vendorId) { Ui.Warn("Select a vendor."); return; }
        if (lines.Count == 0) { Ui.Warn("Add at least one line with an item and quantity."); return; }

        // Adjust these property names to your PurchaseOrder create DTO.
        var body = new
        {
            vendorId,
            poDate = _date.Value.Date,
            remarks = _remarks.Text,
            details = lines.Select(l => new { itemId = l.ItemId, quantity = l.Quantity, rate = l.Rate }),
        };

        await Ui.RunAsync(async () =>
        {
            await _api.RequestAsync<object>(HttpMethod.Post, "api/v1/PurchaseOrder/Create", body);
            DialogResult = DialogResult.OK;
        });
    }
}
