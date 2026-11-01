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

    public static Button Button(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Margin = new Padding(0, 0, 8, 0) };
        b.Click += onClick;
        return b;
    }

    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
    };
}
