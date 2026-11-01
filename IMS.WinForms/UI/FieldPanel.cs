using System.Reflection;
using System.Text.RegularExpressions;

namespace Ims.WinForms.UI;

/// Builds an edit form from a model class using reflection:
/// string -> TextBox, bool -> CheckBox, DateTime -> DateTimePicker, numbers -> NumericUpDown.
public sealed class FieldPanel : TableLayoutPanel
{
    private readonly List<(PropertyInfo Prop, Control Ctl)> _fields = new();

    public FieldPanel(Type modelType)
    {
        ColumnCount = 1;
        AutoSize = true;
        Dock = DockStyle.Top;
        GrowStyle = TableLayoutPanelGrowStyle.AddRows;

        foreach (var p in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite) continue;
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            var ctl = Create(p, t);
            if (ctl is null) continue;

            if (p.Name == modelType.Name + "Id") ctl.Enabled = false;   // identity key is read-only
            ctl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(new Label { Text = Humanize(p.Name), AutoSize = true, Margin = new Padding(0, 8, 0, 2) });
            Controls.Add(ctl);
            _fields.Add((p, ctl));
        }
    }

    static Control? Create(PropertyInfo p, Type t)
    {
        if (t == typeof(string))
            return new TextBox { UseSystemPasswordChar = p.Name.Contains("password", StringComparison.OrdinalIgnoreCase) };
        if (t == typeof(bool)) return new CheckBox { Text = "Yes" };
        if (t == typeof(DateTime))
            return new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = Nullable.GetUnderlyingType(p.PropertyType) is not null };
        if (t == typeof(int) || t == typeof(long) || t == typeof(short))
            return new NumericUpDown { Minimum = -1_000_000_000, Maximum = 1_000_000_000 };
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return new NumericUpDown { DecimalPlaces = 2, Minimum = -1_000_000_000, Maximum = 1_000_000_000 };
        return null;     // unsupported types are skipped
    }

    static string Humanize(string name) => Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

    public void Load(object item)
    {
        foreach (var (p, c) in _fields)
        {
            var v = p.GetValue(item);
            switch (c)
            {
                case TextBox tb: tb.Text = v?.ToString() ?? ""; break;
                case CheckBox cb: cb.Checked = v is true; break;
                case DateTimePicker dp:
                    if (v is DateTime d) { dp.Value = d < dp.MinDate ? dp.MinDate : d; if (dp.ShowCheckBox) dp.Checked = true; }
                    else { dp.Value = DateTime.Today; if (dp.ShowCheckBox) dp.Checked = false; }
                    break;
                case NumericUpDown n:
                    n.Value = v is null ? 0 : Math.Clamp(Convert.ToDecimal(v), n.Minimum, n.Maximum);
                    break;
            }
        }
    }

    public void Save(object item)
    {
        foreach (var (p, c) in _fields)
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            object? v = c switch
            {
                TextBox tb => tb.Text,
                CheckBox cb => cb.Checked,
                DateTimePicker dp => dp.ShowCheckBox && !dp.Checked ? null : dp.Value,
                NumericUpDown n => Convert.ChangeType(n.Value, t),
                _ => null,
            };
            p.SetValue(item, v);
        }
    }
}
