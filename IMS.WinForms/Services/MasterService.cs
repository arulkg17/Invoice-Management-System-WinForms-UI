using System.Reflection;

namespace Ims.WinForms.Services;

/// Generic CRUD client for a master controller:
/// GET GetAll, POST Create, PUT Update/{id}, DELETE Delete/{id}.
public sealed class MasterService<T> where T : class
{
    private readonly ApiClient _api;
    private readonly string _controller;
    private readonly PropertyInfo? _idProp;

    public MasterService(ApiClient api, string controller, string idProperty = "Id")
    {
        _api = api;
        _controller = controller;
        _idProp = typeof(T).GetProperty(idProperty);
    }

    string R(string action) => $"api/v1/{_controller}/{action}";

    public int IdOf(T item) => _idProp?.GetValue(item) is int i ? i : 0;

    public async Task<List<T>> GetAllAsync() =>
        await _api.RequestAsync<List<T>>(HttpMethod.Get, R("GetAll")) ?? new();

    public async Task SaveAsync(T item)
    {
        var id = IdOf(item);
        if (id == 0) await _api.RequestAsync<object>(HttpMethod.Post, R("Create"), item);
        else await _api.RequestAsync<object>(HttpMethod.Put, R($"Update/{id}"), item);
    }

    public async Task DeleteAsync(int id) =>
        await _api.RequestAsync<object>(HttpMethod.Delete, R($"Delete/{id}"));
}
