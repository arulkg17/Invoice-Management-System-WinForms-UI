using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ims.WinForms.UI;

/// Builds an edit form from a model class using reflection:
/// string -> InputBox, bool -> CheckBox, DateTime -> DateTimePicker, numbers -> NumericUpDown,
/// and any property listed in "lookupProps" -> drop-down filled later with SetLookup.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class FieldPanel : TableLayoutPanel
{
    private static readonly HashSet<string> Ignored = new() { "Id", "CreatedBy", "UpdatedBy", "LastLoginDate" };

    private readonly List<(PropertyInfo Prop, Control Ctl)> _fields = new();
    private object? _item;

    public FieldPanel(Type modelType, IEnumerable<string>? lookupProps = null)
    {
        ColumnCount = 1;
        AutoSize = true;
        Dock = DockStyle.Top;
        GrowStyle = TableLayoutPanelGrowStyle.AddRows;
        BackColor = Color.Transparent;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var lookups = new HashSet<string>(lookupProps ?? Array.Empty<string>());

        foreach (var p in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite || Ignored.Contains(p.Name)) continue;
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            Control? ctl = lookups.Contains(p.Name)
                ? new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = Theme.Body }
                : Create(p, t);
            if (ctl is null) continue;

            ctl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(new Label { Text = Humanize(p.Name), AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 6, 0, 2) });
            Controls.Add(ctl);
            _fields.Add((p, ctl));
        }
    }

    static Control? Create(PropertyInfo p, Type t)
    {
        if (t == typeof(string))
            return new InputBox { UseSystemPasswordChar = p.Name.Contains("password", StringComparison.OrdinalIgnoreCase) };
        if (t == typeof(bool)) return new CheckBox { Text = "Yes", AutoSize = true, Font = Theme.Body };
        if (t == typeof(DateTime))
            return new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = Nullable.GetUnderlyingType(p.PropertyType) is not null, Font = Theme.Body };
        if (t == typeof(int) || t == typeof(long) || t == typeof(short))
            return new NumericUpDown { Minimum = -1_000_000_000, Maximum = 1_000_000_000, Font = Theme.Body };
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return new NumericUpDown { DecimalPlaces = 2, Minimum = -1_000_000_000, Maximum = 1_000_000_000, Font = Theme.Body };
        return null;     // unsupported types are skipped
    }

    public static string Humanize(string name) => Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

    /// Fills the drop-down of a property that was listed in lookupProps.
    public void SetLookup(string property, IList items, string valueMember, string displayMember)
    {
        foreach (var (p, c) in _fields)
            if (p.Name == property && c is ComboBox cb)
            {
                cb.DataSource = items;
                cb.ValueMember = valueMember;
                cb.DisplayMember = displayMember;
                cb.SelectedIndex = -1;
                if (_item is not null) LoadOne(p, c, _item);     // the record was shown before the list arrived
            }
    }

    public void Load(object item)
    {
        _item = item;
        foreach (var (p, c) in _fields) LoadOne(p, c, item);
    }

    static void LoadOne(PropertyInfo p, Control c, object item)
    {
        var v = p.GetValue(item);
        switch (c)
        {
            case InputBox ib: ib.Text = v?.ToString() ?? ""; break;
            case CheckBox cb: cb.Checked = v is true; break;
            case ComboBox combo:
                if (v is int id && combo.DataSource is not null) combo.SelectedValue = id;
                else combo.SelectedIndex = -1;
                break;
            case DateTimePicker dp:
                if (v is DateTime d) { dp.Value = d < dp.MinDate ? dp.MinDate : d; if (dp.ShowCheckBox) dp.Checked = true; }
                else { dp.Value = DateTime.Today; if (dp.ShowCheckBox) dp.Checked = false; }
                break;
            case NumericUpDown n:
                n.Value = v is null ? 0 : Math.Clamp(Convert.ToDecimal(v), n.Minimum, n.Maximum);
                break;
        }
    }

    public void Save(object item)
    {
        foreach (var (p, c) in _fields)
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            object? v = c switch
            {
                InputBox ib => ib.Text,
                CheckBox cb => cb.Checked,
                ComboBox combo => Convert.ChangeType(combo.SelectedValue is int i ? i : 0, t),
                DateTimePicker dp => dp.ShowCheckBox && !dp.Checked ? null : dp.Value,
                NumericUpDown n => Convert.ChangeType(n.Value, t),
                _ => null,
            };
            p.SetValue(item, v);
        }
    }
}
