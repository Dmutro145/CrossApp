using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public static IReadOnlyList<Order> Items() =>
    [
        Make("O-BIG",  "C-1", 500m, 4),   // 2000
        Make("O-MID",  "C-2", 300m, 2),   // 600
        Make("O-LOW",  "C-3", 50m,  2),   // 100
        Order.Create("O-ZERO", "C-3")     // 0
    ];

    public static Order Make(string id, string customerId, decimal price, int qty)
    {
        var order = Order.Create(id, customerId);
        order.AddLine("P-1", "Товар", price, qty);
        return order;
    }
}