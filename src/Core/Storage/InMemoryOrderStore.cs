using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryOrderStore(IEnumerable<Order>? seed = null) : IOrderStore
{
    private readonly Dictionary<string, Order> _items =
        (seed ?? []).ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List() => _items.Values.ToList();

    public Order? GetById(string id) => _items.GetValueOrDefault(id);

    public void Update(Order order) => _items[order.Id] = order;

    public bool Remove(string id) => _items.Remove(id);

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (_items.ContainsKey(order.Id))
            throw new DomainException($"Замовлення з id={order.Id} уже існує.");

        _items.Add(order.Id, order);
    }
}


//Extra lab 5.1
public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Order> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Order>? _list;

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public IReadOnlyList<Order> List()
    {
        if (_list is not null) { Hits++; return _list; }

        Misses++;
        _list = _inner.List();
        return _list;
    }

    public Order? GetById(string id)
    {
        if (_byId.TryGetValue(id, out Order? cached)) { Hits++; return cached; }

        Misses++;
        Order? order = _inner.GetById(id);
        if (order is not null) _byId[order.Id] = order;
        return order;
    }

    public void Add(Order order)
    {
        _inner.Add(order);
        _byId[order.Id] = order;
        _list = null;
    }

    public void Update(Order order)
    {
        _inner.Update(order);
        _byId[order.Id] = order;
        _list = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        _byId.Remove(id);
        _list = null;
        return removed;
    }
}