using Core.Abstractions;
using Core.Domain;
//lab6
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    public const int MaxOpenOrdersPerCustomer = 5;

    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    //lab6
    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind) =>
        Changed?.Invoke(this, new OrderChangedEventArgs(
            order.Id, kind, order.Lines.Count, order.Total));

    public Order CreateOrder(string id, string customerId)
    {
        Order order = Order.Create(id, customerId); // інваріанти перевіряє сутність

        int open = _store.List().Count(o =>
            o.CustomerId == order.CustomerId && o.Status == OrderStatus.Draft);

        if (open >= MaxOpenOrdersPerCustomer)
            throw new InvalidOperationException(
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

    private Order GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");
}