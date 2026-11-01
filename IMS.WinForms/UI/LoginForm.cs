using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

[System.ComponentModel.DesignerCategory("Code")]
public sealed class LoginForm : Form
{
    private readonly ApiClient _api;
    private readonly InputBox _user = new() { Width = 340 };
    private readonly InputBox _pwd = new() { Width = 340, UseSystemPasswordChar = true };
    private readonly Label _error = new() { AutoSize = false, Width = 340, Height = 44, ForeColor = Theme.Danger, BackColor = Color.Transparent };
    private readonly ModernButton _signIn;

    public LoginForm(ApiClient api)
    {
        _api = api;

        Text = "IMS - Sign in";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Body;
        BackColor = Theme.AppBg;
        ClientSize = new Size(940, 580);
        MinimumSize = new Size(780, 520);
        StartPosition = FormStartPosition.CenterScreen;

        _signIn = new ModernButton { Text = "Sign in", AutoSize = false, Size = new Size(340, 44), Margin = new Padding(0) };

        // ---- left: branding on the gradient ----
        var left = new BrandPanel { Dock = DockStyle.Fill };

        // ---- right: the form ----
        var form = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
            BackColor = Color.Transparent, Anchor = AnchorStyles.None,
        };
        form.Controls.Add(new Label { Text = "Welcome back", Font = new Font("Segoe UI Semibold", 22F), AutoSize = true, ForeColor = Theme.TextPrimary });
        form.Controls.Add(new Label { Text = "Sign in to continue", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(3, 0, 3, 24) });
        form.Controls.Add(new Label { Text = "User name", AutoSize = true });
        form.Controls.Add(_user);
        form.Controls.Add(new Label { Text = "Password", AutoSize = true });
        form.Controls.Add(_pwd);
        form.Controls.Add(_error);
        form.Controls.Add(_signIn);
        var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.AppBg };
        right.Controls.Add(Centered(form));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(right, 1, 0);
        Controls.Add(layout);

        AcceptButton = _signIn;                       // Enter key signs in
        _signIn.Click += async (_, _) => await SignInAsync();
    }

    /// 3x3 table that keeps its content centred in the middle however large the window is.
    static TableLayoutPanel Centered(Control content)
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, BackColor = Color.Transparent };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        t.Controls.Add(content, 1, 1);
        return t;
    }

    private async Task SignInAsync()
    {
        _error.Text = "";
        if (string.IsNullOrWhiteSpace(_user.Text) || _pwd.Text.Length == 0)
        { _error.Text = "Enter user name and password."; return; }

        _signIn.Enabled = false;
        _signIn.Text = "Signing in...";
        try
        {
            await _api.LoginAsync(_user.Text.Trim(), _pwd.Text);
            DialogResult = DialogResult.OK;           // closes the dialog
        }
        catch (ApiException ex) { _error.Text = ex.Message; }
        catch (HttpRequestException) { _error.Text = "Cannot reach the API. Is Invoice.CoreAPI running?"; }
        catch (TaskCanceledException) { _error.Text = "The request timed out."; }
        finally { _signIn.Enabled = true; _signIn.Text = "Sign in"; }
    }
}
