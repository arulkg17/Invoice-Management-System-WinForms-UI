using Microsoft.Web.WebView2.WinForms;

namespace Ims.WinForms.UI;

/// Shows a PDF with Edge's built-in viewer (print / save included) via WebView2.
public sealed class PdfForm : Form
{
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly string _path;

    public PdfForm(string path, string title)
    {
        _path = path;
        Text = title;
        Size = new Size(900, 800);
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(_web);

        Load += async (_, _) =>
        {
            await _web.EnsureCoreWebView2Async();
            _web.CoreWebView2.Navigate(new Uri(_path).AbsoluteUri);
        };
        FormClosed += (_, _) =>
        {
            _web.Dispose();
            try { File.Delete(_path); } catch { /* temp file */ }
        };
    }

    public static void ShowPdf(IWin32Window? owner, byte[] bytes, string title)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ims_{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, bytes);
        new PdfForm(path, title).Show(owner);
    }
}
