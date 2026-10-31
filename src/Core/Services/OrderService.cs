using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    public const int MaxOpenOrdersPerCustomer = 5;

    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string id, string customerId)
    {
        Order order = Order.Create(id, customerId); // інваріанти перевіряє сутність

        int open = _store.List().Count(o =>
            o.CustomerId == order.CustomerId && o.Status == OrderStatus.Draft);

        if (open >= MaxOpenOrdersPerCustomer)
            throw new InvalidOperationException(
                $"У клієнта {order.CustomerId} уже {open} незавершених замовлень (максимум {MaxOpenOrdersPerCustomer})");

        _store.Add(order); // унікальність id перевіряє сховище
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = GetOrThrow(orderId);
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
    }

    public void Confirm(string orderId)
    {
        Order order = GetOrThrow(orderId);
        order.Confirm();
        _store.Update(order);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    private Order GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");
}