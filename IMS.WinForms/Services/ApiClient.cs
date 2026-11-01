using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ims.WinForms.Services;

public sealed class AuthState
{
    public string? Token { get; set; }
    public string? UserName { get; set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
    public event Action? SessionExpired;
    internal void Expire() { Token = null; SessionExpired?.Invoke(); }
}

public class ApiException(string message, HttpStatusCode status) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public class ApiResponse<T>
{
    public bool? Success { get; set; }
    public bool? IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public sealed class ApiClient(HttpClient http, AuthState auth)
{
    static readonly JsonSerializerOptions Opts = new(JsonSerializerDefaults.Web);

    HttpRequestMessage Build(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (auth.IsLoggedIn) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        if (body is not null) req.Content = JsonContent.Create(body, options: Opts);
        return req;
    }

    void CheckStatus(HttpResponseMessage res, string text)
    {
        if (res.StatusCode == HttpStatusCode.Unauthorized && auth.IsLoggedIn)
        {
            auth.Expire();
            throw new ApiException("Session expired. Please sign in again.", res.StatusCode);
        }
        if (!res.IsSuccessStatusCode)
            throw new ApiException(ExtractMessage(text) ?? $"{(int)res.StatusCode} {res.ReasonPhrase}", res.StatusCode);
    }

    /// Calls the API and returns ApiResponse<T>.Data (the ApiResponse wrapper is unwrapped here).
    public async Task<T?> RequestAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var req = Build(method, url, body);
        using var res = await http.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        CheckStatus(res, text);
        if (string.IsNullOrWhiteSpace(text)) return default;

        var env = JsonSerializer.Deserialize<ApiResponse<T>>(text, Opts);
        if (env is null) return default;
        if (env.Success == false || env.IsSuccess == false)
            throw new ApiException(env.Message ?? "Request failed.", res.StatusCode);
        return env.Data;
    }

    /// POST that returns the raw response text (used by the AI assistant screen).
    public async Task<string> PostRawAsync(string url, object body)
    {
        using var req = Build(HttpMethod.Post, url, body);
        using var res = await http.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        CheckStatus(res, text);
        return text;
    }

    public async Task<byte[]> GetBytesAsync(string url)
    {
        using var req = Build(HttpMethod.Get, url);
        using var res = await http.SendAsync(req);
        if (!res.IsSuccessStatusCode) CheckStatus(res, await res.Content.ReadAsStringAsync());
        return await res.Content.ReadAsByteArrayAsync();
    }

    public async Task LoginAsync(string userName, string password)
    {
        // Adjust the body property names if your UserCredentials model differs.
        using var req = Build(HttpMethod.Post, "api/v1/Login", new { userName, password });
        using var res = await http.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new ApiException(ExtractMessage(text) ?? "Invalid user name or password.", res.StatusCode);

        using var doc = JsonDocument.Parse(text);
        var token = FindToken(doc.RootElement)
                    ?? throw new ApiException("Login succeeded but no token was returned.", res.StatusCode);
        auth.Token = token;
        auth.UserName = userName;
    }

    static string? FindToken(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in e.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.String && p.Name.Contains("token", StringComparison.OrdinalIgnoreCase))
                return p.Value.GetString();
            var inner = FindToken(p.Value);
            if (inner is not null) return inner;
        }
        return null;
    }

    static string? ExtractMessage(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var p in doc.RootElement.EnumerateObject())
                if ((p.Name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
                     p.Name.Equals("title", StringComparison.OrdinalIgnoreCase)) &&
                    p.Value.ValueKind == JsonValueKind.String)
                    return p.Value.GetString();
        }
        catch { }
        return null;
    }
}
