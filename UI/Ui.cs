using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

public static class Ui
{
    /// Runs an API action and shows friendly messages instead of crashing.
    public static async Task RunAsync(Func<Task> action)
    {
        try { await action(); }
        catch (ApiException ex) when (ex.Status == System.Net.HttpStatusCode.Unauthorized) { /* MainForm returns to login */ }
        catch (ApiException ex) { Warn(ex.Message); }
        catch (HttpRequestException) { Warn("Cannot reach the API. Is Invoice.CoreAPI running?"); }
        catch (TaskCanceledException) { Warn("The request timed out."); }
    }

    public static void Warn(string text) =>
        MessageBox.Show(text, "IMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static bool Confirm(string text) =>
        MessageBox.Show(text, "IMS", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public static ModernButton Button(string text, EventHandler onClick, ButtonKind kind = ButtonKind.Primary)
    {
        var b = new ModernButton { Text = text, Kind = kind };
        b.Click += onClick;
        return b;
    }

    /// Read-only styled list grid.
    public static DataGridView Grid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        Theme.StyleGrid(g);
        return g;
    }

    public static Label Heading(string text) => new()
    {
        Text = text, Font = Theme.Heading, ForeColor = Theme.TextPrimary, AutoSize = true, BackColor = Color.Transparent,
        Margin = new Padding(0, 4, 24, 0),
    };

    public static Label Caption(string text) => new()
    {
        Text = text, Font = Theme.Body, ForeColor = Theme.Muted, AutoSize = true, BackColor = Color.Transparent,
    };
}
