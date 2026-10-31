using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Ims.WinForms.Services;

namespace Ims.WinForms.UI;

/// One screen for every master: search, paged grid, add / edit / delete.
public sealed class MasterView<T> : UserControl where T : class, new()
{
    private static readonly PropertyInfo[] StringProps =
        typeof(T).GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();

    private readonly MasterService<T> _service;
    private readonly string _title;
    private const int PageSize = 10;

    private List<T> _all = new();
    private T? _editing;
    private int _page = 1, _totalPages = 1;
    private bool _binding;

    private readonly DataGridView _grid = Ui.Grid();
    private readonly TextBox _search = new() { Width = 260, PlaceholderText = "Search..." };
    private readonly Label _pageLabel = new() { AutoSize = true, Margin = new Padding(8, 7, 0, 0) };
    private readonly Panel _right = new() { Dock = DockStyle.Right, Width = 340, Padding = new Padding(12), AutoScroll = true, Visible = false };
    private readonly FieldPanel _fields = new(typeof(T));

    public MasterView(string title, MasterService<T> service)
    {
        _title = title;
        _service = service;

        // right-hand editor
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 8, 0, 0) };
        buttons.Controls.Add(Ui.Button("Save", async (_, _) => await SaveAsync()));
        buttons.Controls.Add(Ui.Button("Cancel", (_, _) => HideEditor()));
        buttons.Controls.Add(Ui.Button("Delete", async (_, _) => await DeleteAsync()));
        _right.Controls.Add(_fields);
        _right.Controls.Add(buttons);

        // pager
        var pager = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8, 6, 0, 0) };
        pager.Controls.Add(Ui.Button("< Prev", (_, _) => { if (_page > 1) { _page--; Apply(); } }));
        pager.Controls.Add(Ui.Button("Next >", (_, _) => { if (_page < _totalPages) { _page++; Apply(); } }));
        pager.Controls.Add(_pageLabel);

        // toolbar
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 0, 0) };
        bar.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 20, 0) });
        bar.Controls.Add(_search);
        bar.Controls.Add(Ui.Button("Refresh", async (_, _) => await Ui.RunAsync(ReloadAsync)));
        bar.Controls.Add(Ui.Button("New", (_, _) => ShowEditor(new T())));

        // z-order: Fill first, docked edges last
        Controls.Add(_grid);
        Controls.Add(_right);
        Controls.Add(pager);
        Controls.Add(bar);

        _search.TextChanged += (_, _) => { _page = 1; Apply(); };
        _grid.SelectionChanged += (_, _) =>
        {
            if (_binding) return;
            if (_grid.CurrentRow?.DataBoundItem is T item) ShowEditor(Clone(item));
        };
        Load += async (_, _) => await Ui.RunAsync(ReloadAsync);
    }

    static T Clone(T item) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item))!;

    static bool Matches(T item, string text) =>
        StringProps.Any(p => (p.GetValue(item) as string)?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);

    async Task ReloadAsync()
    {
        _all = await _service.GetAllAsync();
        Apply();
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
        _right.Visible = true;
    }

    void HideEditor()
    {
        _editing = null;
        _right.Visible = false;
        _grid.ClearSelection();
    }

    async Task SaveAsync()
    {
        if (_editing is null) return;
        _fields.Save(_editing);

        // the first string property (CategoryName, CustomerName ...) is treated as required
        var key = StringProps.FirstOrDefault();
        if (key is not null && string.IsNullOrWhiteSpace(key.GetValue(_editing) as string))
        { Ui.Warn($"{key.Name} is required."); return; }

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
