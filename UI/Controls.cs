using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Ims.WinForms.UI;

// Custom-drawn controls that give WinForms a modern look. Each one is a class we had to write ourselves;
// in WPF the same effect is a few lines of XAML styling (see Themes/Controls.xaml in the WPF project).

public enum ButtonKind { Primary, Ghost, Danger, Sidebar, Nav }

[System.ComponentModel.DesignerCategory("Code")]
public class ModernButton : Button
{
    private bool _hover, _down, _selected;
    private ButtonKind _kind = ButtonKind.Primary;
    private string _icon = "";

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonKind Kind { get => _kind; set { _kind = value; Invalidate(); } }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Icon { get => _icon; set { _icon = value; Invalidate(); } }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Font = Theme.Bold;
        Cursor = Cursors.Hand;
        Margin = new Padding(0, 0, 8, 0);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Height = 38;
        Theme.Bind(this, Invalidate);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var s = TextRenderer.MeasureText(Text, Font);
        return new Size(s.Width + 40, 38);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color fill, text;
        Color? border = null;
        switch (Kind)
        {
            case ButtonKind.Primary:
                fill = _down ? Theme.Darken(Theme.Accent, .2) : _hover ? Theme.Darken(Theme.Accent, .1) : Theme.Accent;
                text = Color.White; break;
            case ButtonKind.Danger:
                fill = _down ? Theme.Darken(Theme.Danger, .2) : _hover ? Theme.Darken(Theme.Danger, .1) : Theme.Danger;
                text = Color.White; break;
            case ButtonKind.Ghost:
                fill = _hover ? Theme.AccentSoft : Color.Transparent;
                text = Theme.Accent; border = Theme.Accent; break;
            case ButtonKind.Sidebar:
                fill = Color.FromArgb(_hover ? 90 : 38, 255, 255, 255);
                text = Color.White; break;
            default: // Nav
                fill = Selected ? Color.FromArgb(71, 255, 255, 255) : _hover ? Color.FromArgb(38, 255, 255, 255) : Color.Transparent;
                text = Color.White; break;
        }
        if (!Enabled && Kind is ButtonKind.Primary or ButtonKind.Danger) { fill = Theme.Line; text = Theme.Muted; }
        else if (!Enabled) { text = Theme.Muted; }

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Theme.Round(r, 8))
        {
            if (fill.A > 0) { using var b = new SolidBrush(fill); g.FillPath(b, path); }
            if (border is Color bc) { using var p = new Pen(bc, 1.5f); g.DrawPath(p, path); }
        }

        const TextFormatFlags f = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
        if (Kind == ButtonKind.Nav)
        {
            TextRenderer.DrawText(g, Icon, Theme.EmojiSmall, new Rectangle(14, 0, 34, Height), text, f | TextFormatFlags.Left);
            TextRenderer.DrawText(g, Text, Theme.Body, new Rectangle(52, 0, Width - 58, Height), text, f | TextFormatFlags.Left);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text, f | TextFormatFlags.HorizontalCenter);
        }
    }
}

/// White rounded panel with a thin border and a soft shadow line (a "card").
[System.ComponentModel.DesignerCategory("Code")]
public class CardPanel : Panel
{
    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Padding = new Padding(14, 14, 14, 18);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var sp = Theme.Round(new Rectangle(1, 3, Width - 3, Height - 5), 12))
        using (var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)))
            g.FillPath(sb, sp);
        using var cp = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 5), 12);
        using var fill = new SolidBrush(Color.White);
        using var pen = new Pen(Theme.Line);
        g.FillPath(fill, cp);
        g.DrawPath(pen, cp);
    }
}

/// Panel with the sidebar gradient; the gradient follows the accent colour.
[System.ComponentModel.DesignerCategory("Code")]
public class GradientPanel : Panel
{
    public GradientPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Theme.Bind(this, Invalidate);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var rect = new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height));
        using var b = new LinearGradientBrush(rect, Theme.SidebarTop, Theme.SidebarBottom, 90f);
        e.Graphics.FillRectangle(b, rect);
    }
}

/// Rounded text box (a plain TextBox cannot have rounded corners): a panel that paints the border
/// and hosts a borderless TextBox. The border turns to the accent colour while typing.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class InputBox : Panel
{
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Inner { get; } = new() { BorderStyle = BorderStyle.None, BackColor = Color.White, Dock = DockStyle.Fill, Font = Theme.Body };

    public InputBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Padding = new Padding(12, 9, 12, 9);
        Height = Inner.PreferredHeight + 20;
        Margin = new Padding(0, 2, 0, 10);
        Controls.Add(Inner);
        Inner.GotFocus += (_, _) => Invalidate();
        Inner.LostFocus += (_, _) => Invalidate();
        Inner.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        Theme.Bind(this, Invalidate);
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text { get => Inner.Text; set => Inner.Text = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseSystemPasswordChar { get => Inner.UseSystemPasswordChar; set => Inner.UseSystemPasswordChar = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlaceholderText { get => Inner.PlaceholderText; set => Inner.PlaceholderText = value; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        using var fill = new SolidBrush(Color.White);
        using var pen = new Pen(Inner.Focused ? Theme.Accent : Theme.Line, Inner.Focused ? 1.8f : 1f);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }
}

/// Small coloured circle in the sidebar: click it to change the accent colour of the whole app.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class AccentDot : Control
{
    private readonly Color _color;

    public AccentDot(Color color)
    {
        _color = color;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(26, 26);
        Margin = new Padding(0, 0, 8, 0);
        Cursor = Cursors.Hand;
    }

    protected override void OnClick(EventArgs e) { Theme.SetAccent(_color); base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var b = new SolidBrush(_color);
        using var p = new Pen(Color.White, 2f);
        e.Graphics.FillEllipse(b, 2, 2, Width - 5, Height - 5);
        e.Graphics.DrawEllipse(p, 2, 2, Width - 5, Height - 5);
    }
}

/// Icon that floats up and down (login screen).
[System.ComponentModel.DesignerCategory("Code")]
public sealed class FloatingIcon : Control
{
    private readonly WinTimer _timer = new() { Interval = 25 };
    private double _t;

    public FloatingIcon(string icon)
    {
        Text = icon;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = new Font("Segoe UI Emoji", 52F);
        Size = new Size(140, 120);
        Margin = new Padding(0, 0, 0, 12);
        _timer.Tick += (_, _) => { _t += 0.07; Invalidate(); };
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var dy = (int)(Math.Sin(_t) * 10);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(0, dy, Width, Height), Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    protected override void Dispose(bool disposing) { if (disposing) _timer.Dispose(); base.Dispose(disposing); }
}

/// Dashboard number card; the number counts up when the card is first filled.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class KpiCard : CardPanel
{
    private readonly string _icon, _title;
    private readonly WinTimer _timer = new() { Interval = 16 };
    private decimal _target, _value;
    private string _format = "N0";
    private DateTime _start;

    public KpiCard(string icon, string title)
    {
        _icon = icon; _title = title;
        Size = new Size(215, 125);
        Margin = new Padding(0, 0, 16, 16);
        _timer.Tick += (_, _) =>
        {
            var p = Math.Min(1.0, (DateTime.UtcNow - _start).TotalMilliseconds / 700);
            _value = _target * (decimal)(1 - Math.Pow(1 - p, 3));
            if (p >= 1) _timer.Stop();
            Invalidate();
        };
    }

    public void AnimateTo(decimal target, string format = "N0")
    {
        _target = target; _format = format; _start = DateTime.UtcNow; _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, _icon, Theme.EmojiLarge, new Point(14, 10), Theme.TextPrimary);
        TextRenderer.DrawText(e.Graphics, _value.ToString(_format), Theme.Big, new Point(14, 48), Theme.Accent);
        TextRenderer.DrawText(e.Graphics, _title, Theme.Body, new Point(16, 92), Theme.Muted);
    }

    protected override void Dispose(bool disposing) { if (disposing) _timer.Dispose(); base.Dispose(disposing); }
}

/// Horizontal bar chart drawn with GDI+; the bars grow when the data arrives.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class BarChart : Control
{
    private List<(string Label, int Count)> _data = new();
    private double _progress;
    private DateTime _start;
    private readonly WinTimer _timer = new() { Interval = 16 };

    public BarChart()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = Theme.Body;
        Height = 60;
        Theme.Bind(this, Invalidate);
        _timer.Tick += (_, _) =>
        {
            var p = Math.Min(1.0, (DateTime.UtcNow - _start).TotalMilliseconds / 800);
            _progress = 1 - Math.Pow(1 - p, 3);
            if (p >= 1) _timer.Stop();
            Invalidate();
        };
    }

    public void SetData(IEnumerable<(string Label, int Count)> data)
    {
        _data = data.ToList();
        Height = Math.Max(50, _data.Count * 34 + 10);
        _progress = 0; _start = DateTime.UtcNow; _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (_data.Count == 0)
        {
            TextRenderer.DrawText(g, "No data yet", Font, new Point(0, 8), Theme.Muted);
            return;
        }
        const int labelW = 150, rightPad = 60;
        var max = Math.Max(1, _data.Max(d => d.Count));
        var area = Math.Max(10, Width - labelW - rightPad);
        for (var i = 0; i < _data.Count; i++)
        {
            var y = i * 34 + 6;
            TextRenderer.DrawText(g, _data[i].Label, Font, new Rectangle(0, y, labelW - 8, 26), Theme.TextPrimary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            var w = (int)(area * _data[i].Count / (double)max * _progress);
            if (w > 0)
            {
                using var path = Theme.Round(new Rectangle(labelW, y + 3, Math.Max(w, 8), 20), 5);
                using var b = new SolidBrush(Theme.Accent);
                g.FillPath(b, path);
            }
            TextRenderer.DrawText(g, _data[i].Count.ToString(), Theme.Bold, new Point(labelW + Math.Max(w, 8) + 8, y + 3), Theme.TextPrimary);
        }
    }

    protected override void Dispose(bool disposing) { if (disposing) _timer.Dispose(); base.Dispose(disposing); }
}

/// The whole left menu in ONE opaque control: gradient, header, menu items, accent dots and Log out.
/// (Earlier version used many transparent buttons on a gradient; they smeared when repainted. One control that
/// paints everything itself is simpler and always correct.)
[System.ComponentModel.DesignerCategory("Code")]
public sealed class Sidebar : Control
{
    private const int HeaderH = 120, ItemH = 44, Gap = 4, FooterH = 170;
    private static readonly Color[] Palette =
    {
        ColorTranslator.FromHtml("#2563EB"), ColorTranslator.FromHtml("#0D9488"), ColorTranslator.FromHtml("#7C3AED"),
        ColorTranslator.FromHtml("#EA580C"), ColorTranslator.FromHtml("#E11D48"),
    };

    private readonly List<(string Icon, string Title)> _items = new();
    private int _selected = -1, _hover = -1, _scroll;
    private bool _logoutHover;
    private string _userName = "";

    public event Action<int>? ItemClicked;
    public event Action? LogoutClicked;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string UserName { get => _userName; set { _userName = value; Invalidate(); } }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex { get => _selected; set { _selected = value; Invalidate(); } }

    public Sidebar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        Font = Theme.Body;
        Theme.Bind(this, Invalidate);
    }

    public void AddItem(string icon, string title) { _items.Add((icon, title)); Invalidate(); }

    Rectangle NavArea => new(0, HeaderH, Width, Math.Max(0, Height - HeaderH - FooterH));
    Rectangle ItemRect(int i) => new(12, HeaderH + i * (ItemH + Gap) - _scroll, Width - 24, ItemH);
    Rectangle DotRect(int k) => new(24 + k * 34, Height - FooterH + 76, 26, 26);
    Rectangle LogoutRect => new(24, Height - 58, Width - 48, 40);
    int MaxScroll => Math.Max(0, _items.Count * (ItemH + Gap) - NavArea.Height);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var full = new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height));
        using (var br = new LinearGradientBrush(full, Theme.SidebarTop, Theme.SidebarBottom, 90f))
            g.FillRectangle(br, full);

        var muted = Theme.Blend(Theme.SidebarTop, Color.White, 0.72);
        const TextFormatFlags f = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

        // header
        TextRenderer.DrawText(g, "🧾", Theme.EmojiLarge, new Point(18, 10), Color.White);
        TextRenderer.DrawText(g, "Invoice Management", new Font("Segoe UI Semibold", 12.5F), new Rectangle(22, 56, Width - 30, 28), Color.White, f);
        TextRenderer.DrawText(g, "WinForms edition", Theme.Body, new Rectangle(24, 84, Width - 30, 22), muted, f);

        // menu items (clipped to the area between header and footer)
        var area = NavArea;
        g.SetClip(area);
        for (var i = 0; i < _items.Count; i++)
        {
            var r = ItemRect(i);
            if (r.Bottom < area.Top || r.Top > area.Bottom) continue;
            var fill = i == _selected ? Color.FromArgb(71, 255, 255, 255)
                     : i == _hover ? Color.FromArgb(38, 255, 255, 255) : Color.Transparent;
            if (fill.A > 0)
            {
                using var path = Theme.Round(r, 8);
                using var b = new SolidBrush(fill);
                g.FillPath(b, path);
            }
            TextRenderer.DrawText(g, _items[i].Icon, Theme.EmojiSmall, new Rectangle(r.X + 14, r.Y, 34, r.Height), Color.White, f);
            TextRenderer.DrawText(g, _items[i].Title, Theme.Body, new Rectangle(r.X + 52, r.Y, r.Width - 58, r.Height), Color.White, f);
        }
        g.ResetClip();

        // footer
        var y0 = Height - FooterH;
        using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255))) g.DrawLine(pen, 20, y0, Width - 20, y0);
        TextRenderer.DrawText(g, UserName, Theme.Bold, new Rectangle(24, y0 + 12, Width - 40, 26), Color.White, f);
        TextRenderer.DrawText(g, "Accent colour", Theme.Body, new Rectangle(24, y0 + 44, Width - 40, 24), muted, f);
        for (var k = 0; k < Palette.Length; k++)
        {
            var d = DotRect(k);
            using var b = new SolidBrush(Palette[k]);
            using var ring = new Pen(Color.White, Palette[k] == Theme.Accent ? 3f : 1.5f);
            g.FillEllipse(b, d);
            g.DrawEllipse(ring, d);
        }
        var lo = LogoutRect;
        using (var path = Theme.Round(lo, 8))
        using (var b = new SolidBrush(Color.FromArgb(_logoutHover ? 90 : 38, 255, 255, 255)))
            g.FillPath(b, path);
        TextRenderer.DrawText(g, "Log out", Theme.Bold, lo, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    int HitItem(Point p)
    {
        if (!NavArea.Contains(p)) return -1;
        for (var i = 0; i < _items.Count; i++) if (ItemRect(i).Contains(p)) return i;
        return -1;
    }

    int HitDot(Point p)
    {
        for (var k = 0; k < Palette.Length; k++) if (DotRect(k).Contains(p)) return k;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var hover = HitItem(e.Location);
        var logout = LogoutRect.Contains(e.Location);
        Cursor = hover >= 0 || logout || HitDot(e.Location) >= 0 ? Cursors.Hand : Cursors.Default;
        if (hover != _hover || logout != _logoutHover) { _hover = hover; _logoutHover = logout; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1; _logoutHover = false; Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _scroll = Math.Clamp(_scroll - e.Delta / 120 * ItemH, 0, MaxScroll);
        Invalidate();
        base.OnMouseWheel(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var i = HitItem(e.Location);
        if (i >= 0) { ItemClicked?.Invoke(i); return; }
        var k = HitDot(e.Location);
        if (k >= 0) { Theme.SetAccent(Palette[k]); return; }
        if (LogoutRect.Contains(e.Location)) LogoutClicked?.Invoke();
    }
}

/// Login-screen branding panel: gradient + a floating icon, painted as one opaque control.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class BrandPanel : Control
{
    private readonly WinTimer _timer = new() { Interval = 30 };
    private double _t;

    public BrandPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        Theme.Bind(this, Invalidate);
        _timer.Tick += (_, _) => { _t += 0.07; Invalidate(); };
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var full = new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height));
        using (var br = new LinearGradientBrush(full, Theme.SidebarTop, Theme.SidebarBottom, 90f))
            g.FillRectangle(br, full);

        const TextFormatFlags c = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
        var cy = Height / 2;
        var dy = (int)(Math.Sin(_t) * 10);
        TextRenderer.DrawText(g, "🧾", new Font("Segoe UI Emoji", 52F), new Rectangle(0, cy - 150 + dy, Width, 110), Color.White, c);
        TextRenderer.DrawText(g, "Invoice Management", new Font("Segoe UI Semibold", 22F), new Rectangle(0, cy - 30, Width, 44), Color.White, c);
        TextRenderer.DrawText(g, "System", new Font("Segoe UI Semibold", 22F), new Rectangle(0, cy + 12, Width, 44), Color.White, c);
        TextRenderer.DrawText(g, "Masters · Transactions · PDF · AI", Theme.Body, new Rectangle(0, cy + 66, Width, 28),
            Theme.Blend(Theme.SidebarTop, Color.White, 0.75), c);
    }

    protected override void Dispose(bool disposing) { if (disposing) _timer.Dispose(); base.Dispose(disposing); }
}

/// Slides a screen in from the right when the user changes page.
public static class Animator
{
    public static void SlideIn(Control view, Control host, int distance = 40, int frames = 12)
    {
        view.Dock = DockStyle.None;
        view.SetBounds(distance, 0, host.ClientSize.Width, host.ClientSize.Height);
        host.Controls.Add(view);

        var step = 0;
        var timer = new WinTimer { Interval = 15 };
        timer.Tick += (_, _) =>
        {
            if (view.IsDisposed) { timer.Stop(); timer.Dispose(); return; }
            step++;
            var p = Math.Min(1.0, step / (double)frames);
            view.Left = (int)(distance * Math.Pow(1 - p, 3));
            if (p >= 1) { timer.Stop(); timer.Dispose(); view.Dock = DockStyle.Fill; }
        };
        timer.Start();
    }
}
