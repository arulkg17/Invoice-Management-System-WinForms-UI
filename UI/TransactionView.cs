using System.ComponentModel;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// One screen for PO / Receipt / Sales Invoice lists: new, edit, delete, PDF, approve / post / cancel.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class TransactionView<TRow> : UserControl where TRow : class, ITransactionRow
{
    private readonly ApiClient _api;
    private readonly string _controller;
    private readonly string _title;
    private readonly Func<int?, Form>? _entry;
    private readonly Func<List<TRow>, Task>? _enrich;
    private readonly DataGridView _grid = Ui.Grid();

    public TransactionView(ApiClient api, string controller, string title, bool canApprove = false, bool canPost = false,
                           Func<int?, Form>? entry = null, Func<List<TRow>, Task>? enrich = null)
    {
        _api = api; _controller = controller; _title = title; _entry = entry; _enrich = enrich;

        DoubleBuffered = true;
        BackColor = Theme.AppBg;
        Padding = new Padding(24);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent };
        bar.Controls.Add(Ui.Heading(title));
        bar.Controls.Add(Ui.Button("Refresh", async (_, _) => await Ui.RunAsync(LoadAsync), ButtonKind.Ghost));
        if (entry is not null)
        {
            bar.Controls.Add(Ui.Button("＋ New", async (_, _) => await OpenEntryAsync(null)));
            bar.Controls.Add(Ui.Button("✎ Edit", async (_, _) => await EditAsync()));
        }
        bar.Controls.Add(Ui.Button("📄 View PDF", async (_, _) => await ViewPdfAsync()));
        if (canApprove) bar.Controls.Add(Ui.Button("Approve", async (_, _) => await ActionAsync("Approve")));
        if (canPost) bar.Controls.Add(Ui.Button("Post", async (_, _) => await ActionAsync("Post")));
        bar.Controls.Add(Ui.Button("Cancel", async (_, _) => await ActionAsync("Cancel"), ButtonKind.Ghost));
        bar.Controls.Add(Ui.Button("🗑 Delete", async (_, _) => await DeleteAsync(), ButtonKind.Danger));

        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(6, 6, 6, 11) };
        card.Controls.Add(_grid);
        Controls.Add(card);
        Controls.Add(bar);

        // names are shown instead of ids
        _grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in _grid.Columns)
                if (c.DataPropertyName is "Id" or "VendorId" or "CustomerId" or "PurchaseOrderId") c.Visible = false;
        };
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0 && _entry is not null) await EditAsync(); };
        Load += async (_, _) => await Ui.RunAsync(LoadAsync);
    }

    TRow? Selected => _grid.CurrentRow?.DataBoundItem as TRow;

    async Task LoadAsync()
    {
        var rows = await _api.RequestAsync<List<TRow>>(HttpMethod.Get, $"api/v1/{_controller}/GetAll") ?? new();
        if (_enrich is not null) await _enrich(rows);
        _grid.DataSource = new BindingList<TRow>(rows);
    }

    async Task OpenEntryAsync(int? editId)
    {
        using var form = _entry!(editId);
        if (form.ShowDialog(FindForm()) == DialogResult.OK) await Ui.RunAsync(LoadAsync);
    }

    async Task EditAsync()
    {
        if (Selected is null) { Ui.Warn("Select a row first."); return; }
        await OpenEntryAsync(Selected.Id);
    }

    async Task DeleteAsync()
    {
        if (Selected is null) { Ui.Warn("Select a row first."); return; }
        var id = Selected.Id;
        if (!Ui.Confirm($"Delete {_title} #{id}? This cannot be undone.")) return;
        await Ui.RunAsync(async () =>
        {
            await _api.RequestAsync<object>(HttpMethod.Delete, $"api/v1/{_controller}/Delete/{id}");
            await LoadAsync();
        });
    }

    async Task ViewPdfAsync()
    {
        if (Selected is null) { Ui.Warn("Select a row first."); return; }
        var id = Selected.Id;
        await Ui.RunAsync(async () =>
        {
            var bytes = await _api.GetBytesAsync($"api/v1/{_controller}/{id}/pdf");
            PdfForm.ShowPdf(FindForm(), bytes, $"{_title} #{id}");
        });
    }

    async Task ActionAsync(string action)
    {
        if (Selected is null) { Ui.Warn("Select a row first."); return; }
        var id = Selected.Id;
        if (!Ui.Confirm($"{action} #{id}?")) return;
        await Ui.RunAsync(async () =>
        {
            // your controllers: POST api/v1/{controller}/Approve|Post|Cancel/{id}
            await _api.RequestAsync<object>(HttpMethod.Post, $"api/v1/{_controller}/{action}/{id}");
            await LoadAsync();
        });
    }
}
