using Microsoft.Web.WebView2.WinForms;

namespace Ims.WinForms.UI;

/// Shows a PDF with Edge's built-in viewer (print / save included) via WebView2.
/// Has its own header with a Close button and is never larger than the screen.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class PdfForm : Form
{
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly string _path;

    public PdfForm(string path, string title)
    {
        _path = path;
        Text = title;
        Font = Theme.Body;
        BackColor = Theme.AppBg;
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1200, 800);
        Size = new Size(Math.Min(900, (int)(wa.Width * 0.9)), Math.Min(780, (int)(wa.Height * 0.9)));
        MinimumSize = new Size(560, 400);

        var header = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 9, 16, 9), BackColor = Theme.AppBg };
        var close = new ModernButton { Text = "✕ Close", Kind = ButtonKind.Danger, AutoSize = false, Width = 110, Height = 38, Dock = DockStyle.Right };
        close.Click += (_, _) => Close();
        header.Controls.Add(new Label { Text = title, Font = Theme.Heading, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.TextPrimary });
        header.Controls.Add(close);

        Controls.Add(_web);
        Controls.Add(header);

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
