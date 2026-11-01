using System.Drawing.Drawing2D;

namespace Ims.WinForms.UI;

/// Colours, fonts and drawing helpers shared by every custom control.
/// In WPF this whole file is two XAML resource dictionaries; in WinForms we draw it ourselves.
public static class Theme
{
    public static Color Accent { get; private set; } = Color.FromArgb(0x25, 0x63, 0xEB);
    public static event Action? AccentChanged;
    public static void SetAccent(Color c) { Accent = c; AccentChanged?.Invoke(); }

    public static readonly Color AppBg = ColorTranslator.FromHtml("#F3F4F6");
    public static readonly Color Line = ColorTranslator.FromHtml("#E5E7EB");
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#111827");
    public static readonly Color Muted = ColorTranslator.FromHtml("#6B7280");
    public static readonly Color RowAlt = ColorTranslator.FromHtml("#F9FAFB");
    public static readonly Color HeaderBg = ColorTranslator.FromHtml("#F3F4F6");
    public static readonly Color Danger = ColorTranslator.FromHtml("#DC2626");

    public static Color AccentSoft => Blend(Accent, Color.White, 0.86);
    public static Color SidebarTop => Darken(Accent, 0.60);
    public static Color SidebarBottom => Darken(Accent, 0.30);

    public static readonly Font Body = new("Segoe UI", 10F);
    public static readonly Font Bold = new("Segoe UI Semibold", 10F);
    public static readonly Font Heading = new("Segoe UI Semibold", 18F);
    public static readonly Font Big = new("Segoe UI Semibold", 24F);
    public static readonly Font EmojiSmall = new("Segoe UI Emoji", 12F);
    public static readonly Font EmojiLarge = new("Segoe UI Emoji", 20F);

    public static Color Darken(Color c, double f) =>
        Color.FromArgb((int)(c.R * (1 - f)), (int)(c.G * (1 - f)), (int)(c.B * (1 - f)));

    public static Color Blend(Color a, Color b, double t) =>
        Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    public static GraphicsPath Round(Rectangle r, int radius)
    {
        var d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// Runs "apply" now and again every time the accent colour changes (unsubscribes when the control dies).
    public static void Bind(Control c, Action apply)
    {
        apply();
        AccentChanged += apply;
        c.Disposed += (_, _) => AccentChanged -= apply;
    }

    /// Flat, striped grid with a hover highlight (a plain DataGridView looks like Excel 2003 otherwise).
    public static void StyleGrid(DataGridView g)
    {
        g.BorderStyle = BorderStyle.None;
        g.BackgroundColor = Color.White;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = 42;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.GridColor = Line;
        g.RowHeadersVisible = false;
        g.RowTemplate.Height = 38;
        g.AllowUserToResizeRows = false;
        g.Font = Body;

        g.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        g.ColumnHeadersDefaultCellStyle.Font = Bold;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBg;
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

        g.DefaultCellStyle.BackColor = Color.White;
        g.DefaultCellStyle.ForeColor = TextPrimary;
        g.DefaultCellStyle.SelectionForeColor = TextPrimary;
        g.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        g.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
        g.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        g.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextPrimary;

        // selection colour follows the accent colour
        Bind(g, () =>
        {
            g.DefaultCellStyle.SelectionBackColor = AccentSoft;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = AccentSoft;
        });

        g.CellMouseEnter += (_, e) => { if (e.RowIndex >= 0) g.Rows[e.RowIndex].DefaultCellStyle.BackColor = AccentSoft; };
        g.CellMouseLeave += (_, e) => { if (e.RowIndex >= 0 && e.RowIndex < g.Rows.Count) g.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Empty; };
    }
}
