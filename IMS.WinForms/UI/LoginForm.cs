using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

public sealed class LoginForm : Form
{
    private readonly ApiClient _api;
    private readonly TextBox _user = new() { Width = 290 };
    private readonly TextBox _pwd = new() { Width = 290, UseSystemPasswordChar = true };
    private readonly Label _error = new() { AutoSize = false, Width = 290, Height = 36, ForeColor = Color.Firebrick };
    private readonly Button _signIn = new() { Text = "Sign in", Width = 290, Height = 32 };

    public LoginForm(ApiClient api)
    {
        _api = api;
        Text = "IMS - Sign in";
        ClientSize = new Size(350, 530);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        MaximizeBox = false;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(28, 24, 28, 12),
        };
        panel.Controls.Add(new Label { Text = "Invoice Management", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 16) });
        panel.Controls.Add(new Label { Text = "User name", AutoSize = true });
        panel.Controls.Add(_user);
        panel.Controls.Add(new Label { Text = "Password", AutoSize = true, Margin = new Padding(3, 12, 3, 0) });
        panel.Controls.Add(_pwd);
        panel.Controls.Add(_error);
        panel.Controls.Add(_signIn);
        Controls.Add(panel);

        AcceptButton = _signIn;          // Enter key signs in
        _signIn.Click += async (_, _) => await SignInAsync();
    }

    private async Task SignInAsync()
    {
        _error.Text = "";
        if (string.IsNullOrWhiteSpace(_user.Text) || _pwd.Text.Length == 0)
        { _error.Text = "Enter user name and password."; return; }

        _signIn.Enabled = false;
        try
        {
            await _api.LoginAsync(_user.Text.Trim(), _pwd.Text);
            DialogResult = DialogResult.OK;     // closes the dialog
        }
        catch (ApiException ex) { _error.Text = ex.Message; }
        catch (HttpRequestException) { _error.Text = "Cannot reach the API. Is Invoice.CoreAPI running?"; }
        catch (TaskCanceledException) { _error.Text = "The request timed out."; }
        finally { _signIn.Enabled = true; }
    }

    private void InitializeComponent()
    {

    }
}
