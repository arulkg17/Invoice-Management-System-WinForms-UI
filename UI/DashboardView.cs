using System.Text.Json;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// Landing page: KPI cards (numbers count up) + a purchase-order-by-status bar chart (bars grow).
[System.ComponentModel.DesignerCategory("Code")]
public sealed class DashboardView : UserControl
{
    private readonly ApiClient _api;
    private readonly KpiCard _categories = new("🗂", "Categories");
    private readonly KpiCard _customers = new("👥", "Customers");
    private readonly KpiCard _vendors = new("🏭", "Vendors");
    private readonly KpiCard _items = new("📦", "Items");
    private readonly KpiCard _orders = new("🛒", "Purchase orders");
    private readonly KpiCard _value = new("💰", "PO value");
    private readonly BarChart _chart = new() { Dock = DockStyle.Top };
    private readonly CardPanel _chartCard = new() { Dock = DockStyle.Top, Height = 140, Padding = new Padding(18, 14, 18, 18) };

    public DashboardView(ApiClient api)
    {
        _api = api;
        DoubleBuffered = true;
        BackColor = Theme.AppBg;
        Padding = new Padding(24);
        AutoScroll = true;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        flow.Controls.AddRange(new Control[] { _categories, _customers, _vendors, _items, _orders, _value });

        _chartCard.Controls.Add(_chart);
        _chartCard.Controls.Add(new Label { Text = "Purchase orders by status", Font = Theme.Bold, Dock = DockStyle.Top, Height = 32, ForeColor = Theme.TextPrimary });

        // add bottom-up: Dock.Top controls stack with the last one added on top
        Controls.Add(_chartCard);
        Controls.Add(flow);
        Controls.Add(new Label { Text = "Dashboard", Font = Theme.Heading, Dock = DockStyle.Top, Height = 50, ForeColor = Theme.TextPrimary });

        Load += async (_, _) => await Ui.RunAsync(LoadAsync);
    }

    async Task LoadAsync()
    {
        async Task<int> CountAsync(string controller) =>
            (await _api.RequestAsync<List<JsonElement>>(HttpMethod.Get, $"api/v1/{controller}/GetAll"))?.Count ?? 0;

        var cat = CountAsync("Category");
        var cus = CountAsync("Customer");
        var ven = CountAsync("Vendor");
        var itm = CountAsync("Itemmaster");
        var posTask = _api.RequestAsync<List<PurchaseOrderRow>>(HttpMethod.Get, "api/v1/PurchaseOrder/GetAll");
        await Task.WhenAll(cat, cus, ven, itm, posTask);
        var pos = posTask.Result ?? new();

        _categories.AnimateTo(cat.Result);
        _customers.AnimateTo(cus.Result);
        _vendors.AnimateTo(ven.Result);
        _items.AnimateTo(itm.Result);
        _orders.AnimateTo(pos.Count);
        _value.AnimateTo(pos.Sum(p => p.TotalAmount));

        var groups = pos.GroupBy(p => string.IsNullOrWhiteSpace(p.Status) ? "Unknown" : p.Status!)
                        .OrderByDescending(g => g.Count()).ToList();
        _chart.SetData(groups.Select(g => (g.Key, g.Count())));
        _chartCard.Height = _chart.Height + 70;
    }
}
