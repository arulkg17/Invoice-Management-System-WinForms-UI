using System.Reflection;

namespace Ims.WinForms.Services;

/// Generic CRUD client for a master controller. Routes follow your Category pattern:
/// GetAll / Create / Update / Delete/{id}. Change them here if yours differ.
public sealed class MasterService<T> where T : class
{
    private readonly ApiClient _api;
    private readonly string _controller;
    private readonly PropertyInfo? _idProp;

    public MasterService(ApiClient api, string controller, string? idProperty = null)
    {
        _api = api;
        _controller = controller;
        _idProp = typeof(T).GetProperty(idProperty ?? typeof(T).Name + "Id");
    }

    string R(string action) => $"api/v1/{_controller}/{action}";

    public int IdOf(T item) => _idProp?.GetValue(item) is int i ? i : 0;

    public async Task<List<T>> GetAllAsync() =>
        await _api.RequestAsync<List<T>>(HttpMethod.Get, R("GetAll")) ?? new();

    public async Task SaveAsync(T item)
    {
        if (IdOf(item) == 0) await _api.RequestAsync<object>(HttpMethod.Post, R("Create"), item);
        else await _api.RequestAsync<object>(HttpMethod.Put, R("Update"), item);
    }

    public async Task DeleteAsync(int id) =>
        await _api.RequestAsync<object>(HttpMethod.Delete, R($"Delete/{id}"));
}
