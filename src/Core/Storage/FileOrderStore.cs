using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            var dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path), Options) ?? [];
            foreach (var dto in dtos)
            {
                var order = Order.FromDto(dto);
                _cache[order.Id] = order;
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(o => o.ToDto()).ToList(), Options));
    }

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureLoaded();

        if (_cache.ContainsKey(order.Id))
            throw new InvalidOperationException($"Замовлення з id={order.Id} уже існує.");

        _cache.Add(order.Id, order);
        Flush();
    }

    public void Update(Order order)
    {
        EnsureLoaded();
        _cache[order.Id] = order;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}