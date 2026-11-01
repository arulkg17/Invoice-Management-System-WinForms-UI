using System.ComponentModel;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// One screen for PO / Receipt / Sales Invoice lists: refresh, new, PDF, approve / post / cancel.
public sealed class TransactionView<TRow> : UserControl where TRow : class, ITransactionRow
{
    private readonly ApiClient _api;
    private readonly string _controller;
    private readonly string _title;
    private readonly Func<Form>? _newEntry;
    private readonly DataGridView _grid = Ui.Grid();

    public TransactionView(ApiClient api, string controller, string title,
                           bool canApprove = false, bool canPost = false, Func<Form>? newEntry = null)
    {
        _api = api; _controller = controller; _title = title; _newEntry = newEntry;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 0, 0) };
        bar.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 20, 0) });
        bar.Controls.Add(Ui.Button("Refresh", async (_, _) => await Ui.RunAsync(LoadAsync)));
        if (newEntry is not null) bar.Controls.Add(Ui.Button("New", async (_, _) => await NewAsync()));
        bar.Controls.Add(Ui.Button("View PDF", async (_, _) => await ViewPdfAsync()));
        if (canApprove) bar.Controls.Add(Ui.Button("Approve", async (_, _) => await ActionAsync("Approve")));
        if (canPost) bar.Controls.Add(Ui.Button("Post", async (_, _) => await ActionAsync("Post")));
        bar.Controls.Add(Ui.Button("Cancel", async (_, _) => await ActionAsync("Cancel")));

        Controls.Add(_grid);
        Controls.Add(bar);
        Load += async (_, _) => await Ui.RunAsync(LoadAsync);
    }

    TRow? Selected => _grid.CurrentRow?.DataBoundItem as TRow;

    async Task LoadAsync()
    {
        var rows = await _api.RequestAsync<List<TRow>>(HttpMethod.Get, $"api/v1/{_controller}/GetAll") ?? new();
        _grid.DataSource = new BindingList<TRow>(rows);
    }

    async Task NewAsync()
    {
        using var form = _newEntry!();
        if (form.ShowDialog(FindForm()) == DialogResult.OK) await Ui.RunAsync(LoadAsync);
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
            await _api.RequestAsync<object>(HttpMethod.Post, $"api/v1/{_controller}/{id}/{action}");
            await LoadAsync();
        });
    }
}
