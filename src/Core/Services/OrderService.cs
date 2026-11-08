using Core.Abstractions;
using Core.Domain;
//lab6
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, params INotificationSink[] sinks)
{
    public const int MaxOpenOrdersPerCustomer = 5;

    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    //lab6
    public event EventHandler<OrderChangedEventArgs>? Changed;

   private void OnChanged(Order order, OrderChangeKind kind)
    {
        var args = new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total);
        Changed?.Invoke(this, args);   // подія: підписники в Cli
        foreach (var sink in _sinks)   // sink'и з конструктора
            sink.Notify(args);
    }
    //lab8
    public async Task<int> ImportAsync(IOrderImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var order = items[i];
            if (_store.GetById(order.Id) is null)          // ідемпотентність імпорту
            {
                _store.Add(order);
                OnChanged(order, OrderChangeKind.OrderCreated);
                added++;
            }
            progress?.Report((i + 1) * 100 / items.Count); // відсотки
        }
        return added;
    }

    //lab8:extra1
    public async Task<int> ImportStreamAsync(IOrderStreamImporter importer, string path,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var added = 0;
        await foreach (var order in importer.StreamAsync(path).WithCancellation(ct))
        {
            if (_store.GetById(order.Id) is not null) continue;
            _store.Add(order);
            OnChanged(order, OrderChangeKind.OrderCreated);
            added++;
        }
        return added;
    }

    //lab8:extra2
    public async Task<int> ImportWithTimeoutAsync(IOrderImporter importer, string path,
        TimeSpan timeout, CancellationToken userToken = default, IProgress<int>? progress = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);
        try
        {
            return await ImportAsync(importer, path, linked.Token, progress);
        }
        catch (OperationCanceledException)
            when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт не вклався у {timeout.TotalMilliseconds:F0} мс.");
        }
    }
    public Order CreateOrder(string id, string customerId)
    {
        Order order = Order.Create(id, customerId); // інваріанти перевіряє сутність

        int open = _store.List().Count(o =>
            o.CustomerId == order.CustomerId && o.Status == OrderStatus.Draft);

        if (open >= MaxOpenOrdersPerCustomer)
            throw new DomainException(
                $"У клієнта {order.CustomerId} уже {open} незавершених замовлень (максимум {MaxOpenOrdersPerCustomer})");

        _store.Add(order); // унікальність id перевіряє сховище
        OnChanged(order, OrderChangeKind.OrderCreated); //lab6: після зміни, не до
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = GetOrThrow(orderId);
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded); //lab6
    }

    public void Confirm(string orderId)
    {
        Order order = GetOrThrow(orderId);
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed); //lab6
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    //Extra lab5.2
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
   //Extra lab6.1
   private readonly INotificationSink[] _sinks = sinks;

    private Order GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new DomainException($"Немає замовлення з id={id}.");
}