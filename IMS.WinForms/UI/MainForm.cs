using Ims.WinForms.Models;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

public sealed class MainForm : Form
{
    private sealed class NavItem(string title, Func<Control> create)
    {
        public Func<Control> Create { get; } = create;
        public override string ToString() => title;
    }

    private readonly AuthState _auth;
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly ListBox _nav = new();
    private readonly Action _onExpired;

    public bool LoggedOut { get; private set; }

    public MainForm(ApiClient api, AuthState auth)
    {
        _auth = auth;
        Text = "Invoice Management System";
        Size = new Size(1280, 760);
        StartPosition = FormStartPosition.CenterScreen;

        var left = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Color.FromArgb(31, 41, 55) };
        _nav.Dock = DockStyle.Fill;
        _nav.BorderStyle = BorderStyle.None;
        _nav.BackColor = left.BackColor;
        _nav.ForeColor = Color.White;
        _nav.Font = new Font("Segoe UI", 11);
        _nav.Margin = new Padding(0);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 84, Padding = new Padding(12) };
        bottom.Controls.Add(Ui.Button("Log out", (_, _) => Logout()));
        bottom.Controls.Add(new Label { Text = auth.UserName ?? "", ForeColor = Color.White, Dock = DockStyle.Top, Height = 26 });
        left.Controls.Add(_nav);
        left.Controls.Add(bottom);

        Controls.Add(_content);
        Controls.Add(left);

        // one line per screen: title + how to build it
        _nav.Items.Add(new NavItem("Categories",      () => new MasterView<Category>("Categories", new MasterService<Category>(api, "Category"))));
        _nav.Items.Add(new NavItem("Customers",       () => new MasterView<Customer>("Customers", new MasterService<Customer>(api, "Customer"))));
        _nav.Items.Add(new NavItem("Vendors",         () => new MasterView<Vendor>("Vendors", new MasterService<Vendor>(api, "Vendor"))));
        _nav.Items.Add(new NavItem("Item Master",     () => new MasterView<Itemmaster>("Item Master", new MasterService<Itemmaster>(api, "Itemmaster"))));
        _nav.Items.Add(new NavItem("Users",           () => new MasterView<User>("Users", new MasterService<User>(api, "User"))));
        _nav.Items.Add(new NavItem("Purchase Orders", () => new TransactionView<PurchaseOrderRow>(api, "PurchaseOrder", "Purchase Orders", canApprove: true, newEntry: () => new PurchaseOrderEntryForm(api))));
        _nav.Items.Add(new NavItem("Receipts",        () => new TransactionView<ReceiptRow>(api, "Receipt", "Receipts")));
        _nav.Items.Add(new NavItem("Sales Invoices",  () => new TransactionView<SalesInvoiceRow>(api, "SalesInvoice", "Sales Invoices", canPost: true)));
        _nav.Items.Add(new NavItem("AI Assistant",    () => new AiView(api)));

        _nav.SelectedIndexChanged += (_, _) => Navigate();
        Load += (_, _) => _nav.SelectedIndex = 0;

        // 401 from the API -> back to the login screen
        _onExpired = () => BeginInvoke(() => { LoggedOut = true; Close(); });
        auth.SessionExpired += _onExpired;
        FormClosed += (_, _) => auth.SessionExpired -= _onExpired;
    }

    void Navigate()
    {
        if (_nav.SelectedItem is not NavItem item) return;
        var old = _content.Controls.Cast<Control>().ToList();
        _content.Controls.Clear();
        old.ForEach(c => c.Dispose());

        var view = item.Create();
        view.Dock = DockStyle.Fill;
        _content.Controls.Add(view);
    }

    void Logout()
    {
        _auth.Token = null;
        LoggedOut = true;
        Close();
    }
}
