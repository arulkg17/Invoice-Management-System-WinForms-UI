using System.Globalization;
using System.Text.Json;

namespace Ims.WinForms.UI;

/// Reads values from an API response (JsonElement) without needing a model class.
internal static class JsonHelp
{
    public static JsonElement? Find(JsonElement e, params string[] names)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in e.EnumerateObject())
            if (names.Any(n => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase))) return p.Value;
        return null;
    }

    public static decimal GetDec(JsonElement e, string name)
    {
        var v = Find(e, name);
        if (v is null) return 0;
        if (v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetDecimal(out var d)) return d;
        return decimal.TryParse(v.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0;
    }

    public static int GetInt(JsonElement e, string name) => (int)GetDec(e, name);

    public static DateTime? GetDate(JsonElement e, string name) =>
        Find(e, name) is { ValueKind: not JsonValueKind.Null } v &&
        DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
