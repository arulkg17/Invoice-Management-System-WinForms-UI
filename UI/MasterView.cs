using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// A drop-down fed from another API list (e.g. Category for an item).
public sealed record LookupSpec(string Property, Func<Task<IList>> Load, string ValueMember, string DisplayMember);

/// One screen for every master: search, paged grid, add / edit / delete.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class MasterView<T> : UserControl where T : class, new()
{
    private static readonly PropertyInfo[] TextProps =
        typeof(T).GetProperties().Where(p => p.PropertyType == typeof(string) && p.Name != "Password").ToArray();

    // columns that stay in the edit form but are too wide / sensitive for the grid
    private static readonly HashSet<string> Hidden = new()
    {
        "Password", "CreatedBy", "UpdatedBy", "MiddleName", "AlternatePhone", "AddressLine1", "AddressLine2",
        "DateOfBirth", "Address1", "Address2", "Country", "ZipCode", "GstNo",
        "MinimumStock", "MaximumStock", "ItemBarCode", "ContactPerson",
    };

    private const int PageSize = 10;

    private readonly MasterService<T> _service;
    private readonly string _title;
    private readonly string[] _required;
    private readonly Func<T, string?>? _validate;
    private readonly Action<T>? _prepare;
    private readonly LookupSpec[] _lookups;

    private List<T> _all = new();
    private T? _editing;
    private int _page = 1, _totalPages = 1;
    private bool _binding, _lookupsLoaded;

    private readonly DataGridView _grid = Ui.Grid();
    private readonly InputBox _search = new() { Width = 280, PlaceholderText = "Search..." };
    private readonly Label _pageLabel = Ui.Caption("");
    private readonly CardPanel _right = new() { Dock = DockStyle.Right, Width = 430, Visible = false };
    private readonly Panel _spacer = new() { Dock = DockStyle.Right, Width = 16, Visible = false, BackColor = Color.Transparent };
    private readonly FieldPanel _fields;

    public MasterView(string title, MasterService<T> service, string[] requiredFields,
                      Func<T, string?>? validate = null, Action<T>? prepare = null, LookupSpec[]? lookups = null)
    {
        _title = title; _service = service; _required = requiredFields;
        _validate = validate; _prepare = prepare; _lookups = lookups ?? Array.Empty<LookupSpec>();
        _fields = new FieldPanel(typeof(T), _lookups.Select(l => l.Property));

        DoubleBuffered = true;
        BackColor = Theme.AppBg;
        Padding = new Padding(24);

        // right-hand editor card: heading, scrolling form, buttons
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };
        scroll.Controls.Add(_fields);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(0, 10, 0, 0), BackColor = Color.Transparent };
        buttons.Controls.Add(Ui.Button("Save", async (_, _) => await SaveAsync()));
        buttons.Controls.Add(Ui.Button("Cancel", (_, _) => HideEditor(), ButtonKind.Ghost));
        buttons.Controls.Add(Ui.Button("Delete", async (_, _) => await DeleteAsync(), ButtonKind.Danger));
        var heading = new Label { Text = "Details", Font = Theme.Bold, Dock = DockStyle.Top, Height = 32, ForeColor = Theme.TextPrimary, BackColor = Color.Transparent };
        _right.Controls.Add(scroll);
        _right.Controls.Add(buttons);
        _right.Controls.Add(heading);

        // grid card
        var gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(6, 6, 6, 11) };
        gridCard.Controls.Add(_grid);

        // pager
        var pager = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(0, 14, 0, 0), BackColor = Color.Transparent };
        pager.Controls.Add(Ui.Button("‹ Prev", (_, _) => { if (_page > 1) { _page--; Apply(); } }, ButtonKind.Ghost));
        pager.Controls.Add(Ui.Button("Next ›", (_, _) => { if (_page < _totalPages) { _page++; Apply(); } }, ButtonKind.Ghost));
        _pageLabel.Margin = new Padding(8, 8, 0, 0);
        pager.Controls.Add(_pageLabel);

        // toolbar
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent };
        bar.Controls.Add(Ui.Heading(title));
        bar.Controls.Add(_search);
        bar.Controls.Add(Ui.Button("Refresh", async (_, _) => await Ui.RunAsync(ReloadAsync), ButtonKind.Ghost));
        bar.Controls.Add(Ui.Button("＋ New", (_, _) => ShowEditor(new T())));

        // z-order: Fill first, docked edges last
        Controls.Add(gridCard);
        Controls.Add(_spacer);
        Controls.Add(_right);
        Controls.Add(pager);
        Controls.Add(bar);

        _search.TextChanged += (_, _) => { _page = 1; Apply(); };
        _grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in _grid.Columns) if (Hidden.Contains(c.DataPropertyName)) c.Visible = false;
        };
        _grid.SelectionChanged += (_, _) =>
        {
            if (_binding) return;
            if (_grid.CurrentRow?.DataBoundItem is T item) ShowEditor(Clone(item));
        };
        Load += async (_, _) => await Ui.RunAsync(ReloadAsync);
    }

    static T Clone(T item) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item))!;

    static bool Matches(T item, string text) =>
        TextProps.Any(p => (p.GetValue(item) as string)?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);

    async Task ReloadAsync()
    {
        _all = await _service.GetAllAsync();
        Apply();
        if (!_lookupsLoaded)
        {
            foreach (var l in _lookups) _fields.SetLookup(l.Property, await l.Load(), l.ValueMember, l.DisplayMember);
            _lookupsLoaded = true;
        }
    }

    void Apply()
    {
        var text = _search.Text.Trim();
        var filtered = text.Length == 0 ? _all : _all.Where(x => Matches(x, text)).ToList();
        _totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        if (_page > _totalPages) _page = _totalPages;

        _binding = true;
        _grid.DataSource = new BindingList<T>(filtered.Skip((_page - 1) * PageSize).Take(PageSize).ToList());
        _grid.CurrentCell = null;
        _grid.ClearSelection();
        _binding = false;

        _pageLabel.Text = $"Page {_page} / {_totalPages}   ({filtered.Count} records)";
    }

    void ShowEditor(T item)
    {
        _editing = item;
        _fields.Load(item);
        _right.Visible = _spacer.Visible = true;
    }

    void HideEditor()
    {
        _editing = null;
        _right.Visible = _spacer.Visible = false;
        _grid.ClearSelection();
    }

    async Task SaveAsync()
    {
        if (_editing is null) return;
        _fields.Save(_editing);

        foreach (var name in _required)
            if (string.IsNullOrWhiteSpace(typeof(T).GetProperty(name)?.GetValue(_editing) as string))
            { Ui.Warn($"{FieldPanel.Humanize(name)} is required."); return; }

        var problem = _validate?.Invoke(_editing);
        if (problem is not null) { Ui.Warn(problem); return; }

        _prepare?.Invoke(_editing);
        var item = _editing;
        await Ui.RunAsync(async () =>
        {
            await _service.SaveAsync(item);
            HideEditor();
            await ReloadAsync();
        });
    }

    async Task DeleteAsync()
    {
        if (_editing is null || _service.IdOf(_editing) == 0) return;
        if (!Ui.Confirm($"Delete this record from {_title}?")) return;
        var id = _service.IdOf(_editing);
        await Ui.RunAsync(async () =>
        {
            await _service.DeleteAsync(id);
            HideEditor();
            await ReloadAsync();
        });
    }
}
