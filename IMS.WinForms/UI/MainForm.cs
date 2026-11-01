using System.Collections;
using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

[System.ComponentModel.DesignerCategory("Code")]
public sealed class MainForm : Form
{
    private readonly ApiClient _api;
    private readonly AuthState _auth;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Theme.AppBg };
    private readonly Sidebar _side = new() { Dock = DockStyle.Left, Width = 250 };
    private readonly List<Func<Control>> _screens = new();
    private readonly Action _onExpired;

    public bool LoggedOut { get; private set; }

    public MainForm(ApiClient api, AuthState auth)
    {
        _api = api; _auth = auth;
        Text = "Invoice Management System";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Body;
        BackColor = Theme.AppBg;
        Size = new Size(1320, 780);
        MinimumSize = new Size(1000, 620);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        // ---- sidebar: one control paints the gradient, menu, accent dots and Log out ----
        _side.UserName = auth.UserName ?? "";
        _side.ItemClicked += i => { _side.SelectedIndex = i; Navigate(_screens[i]); };
        _side.LogoutClicked += Logout;
        Controls.Add(_content);
        Controls.Add(_side);

        // ---- one line per screen: icon, title, how to build it ----
        AddNav("📊", "Dashboard", () => new DashboardView(api));
        AddNav("🗂", "Categories", () => new MasterView<Category>("Categories", new MasterService<Category>(api, "Category"),
            requiredFields: new[] { "Code", "Name" }));
        AddNav("👥", "Customers", () => new MasterView<Customer>("Customers", new MasterService<Customer>(api, "Customer"),
            requiredFields: new[] { "CustomerCode", "CustomerName" }));
        AddNav("🏭", "Vendors", () => new MasterView<Vendor>("Vendors", new MasterService<Vendor>(api, "Vendor"),
            requiredFields: new[] { "VendorCode", "VendorName" }));
        AddNav("📦", "Item Master", () => new MasterView<Itemmaster>("Item Master", new MasterService<Itemmaster>(api, "Itemmaster"),
            requiredFields: new[] { "ItemCode", "ItemName", "Uom" },
            validate: i => i.CategoryId == 0 ? "Select a category." : null,
            lookups: new[] { new LookupSpec("CategoryId",
                async () => (IList)await new MasterService<Category>(api, "Category").GetAllAsync(), "Id", "Name") }));
        AddNav("👤", "Users", () => new MasterView<User>("Users", new MasterService<User>(api, "User"),
            requiredFields: new[] { "UserName", "Email", "FirstName", "LastName" },
            validate: u => u.Id == 0 && string.IsNullOrEmpty(u.Password) ? "Password is required for a new user." : null,
            prepare: u =>
            {
                var me = auth.UserName ?? "admin";
                if (u.Id == 0) u.CreatedBy = me;                                   // UserCreateDto.CreatedBy
                else { u.UpdatedBy = me; if (string.IsNullOrEmpty(u.Password)) u.Password = null; }   // null = keep the password
            }));
        AddNav("🛒", "Purchase Orders", () => new TransactionView<PurchaseOrderRow>(api, "PurchaseOrder", "Purchase Orders",
            canApprove: true,
            entry: id => new DocumentEntryForm(api, DocumentConfigs.PurchaseOrder, id),
            enrich: EnrichOrdersAsync));
        AddNav("📥", "Receipts", () => new TransactionView<ReceiptRow>(api, "Receipt", "Receipts",
            canPost: true,
            entry: id => new ReceiptEntryForm(api, id),
            enrich: EnrichReceiptsAsync));
        AddNav("🧾", "Sales Invoices", () => new TransactionView<SalesInvoiceRow>(api, "SalesInvoice", "Sales Invoices",
            canPost: true,
            entry: id => new DocumentEntryForm(api, DocumentConfigs.SalesInvoice, id),
            enrich: EnrichInvoicesAsync));
        AddNav("🤖", "AI Assistant", () => new AiView(api));

        Load += (_, _) => { _side.SelectedIndex = 0; Navigate(_screens[0]); };

        // 401 from the API -> back to the login screen
        _onExpired = () => BeginInvoke(() => { LoggedOut = true; Close(); });
        auth.SessionExpired += _onExpired;
        FormClosed += (_, _) => auth.SessionExpired -= _onExpired;
    }

    void AddNav(string icon, string title, Func<Control> create)
    {
        _side.AddItem(icon, title);
        _screens.Add(create);
    }

    void Navigate(Func<Control> create)
    {
        var old = _content.Controls.Cast<Control>().ToList();
        _content.Controls.Clear();
        old.ForEach(c => c.Dispose());
        Animator.SlideIn(create(), _content);          // fade-in equivalent: the page slides in from the right
    }

    void Logout()
    {
        _auth.Token = null;
        LoggedOut = true;
        Close();
    }

    // ---- show names instead of ids in the transaction lists ----
    async Task EnrichOrdersAsync(List<PurchaseOrderRow> rows)
    {
        var vendors = (await new MasterService<Vendor>(_api, "Vendor").GetAllAsync())
            .GroupBy(v => v.Id).ToDictionary(g => g.Key, g => g.First().VendorName);
        foreach (var r in rows) r.Vendor = vendors.GetValueOrDefault(r.VendorId);
    }

    async Task EnrichReceiptsAsync(List<ReceiptRow> rows)
    {
        var vendors = (await new MasterService<Vendor>(_api, "Vendor").GetAllAsync())
            .GroupBy(v => v.Id).ToDictionary(g => g.Key, g => g.First().VendorName);
        var pos = (await _api.RequestAsync<List<PurchaseOrderRow>>(HttpMethod.Get, "api/v1/PurchaseOrder/GetAll") ?? new())
            .GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First().PONumber);
        foreach (var r in rows)
        {
            r.Vendor = vendors.GetValueOrDefault(r.VendorId);
            r.PurchaseOrder = pos.GetValueOrDefault(r.PurchaseOrderId);
        }
    }

    async Task EnrichInvoicesAsync(List<SalesInvoiceRow> rows)
    {
        var customers = (await new MasterService<Customer>(_api, "Customer").GetAllAsync())
            .GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First().CustomerName);
        foreach (var r in rows) r.Customer = customers.GetValueOrDefault(r.CustomerId);
    }
}
