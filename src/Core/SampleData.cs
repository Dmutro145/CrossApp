using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{   //lab7:Extra1
    public static IReadOnlyList<CustomerDto> Customers() =>
    [
        new("C-100", "ТОВ «Будмайстер»"),
        new("C-101", "ФОП Іваненко"),
        new("C-102", "ТОВ «Домобуд»"),
        new("C-103", "ФОП Петренко"),
        new("C-104", "ТОВ «Ремонт-Сервіс»"),
        new("C-105", "ФОП Коваленко"),
        new("C-106", "ТОВ «Новий клієнт»"),
    ];

    public static IReadOnlyList<Order> Orders() =>
    [
        Make("O-001", "C-100", OrderStatus.Confirmed, ("P-001", "Цемент М400 25кг", 185.50m, 10), ("P-002", "Пісок будівельний", 95m, 3)),
        Make("O-002", "C-100", OrderStatus.Confirmed, ("P-003", "Цегла червона", 12.30m, 500)),
        Make("O-003", "C-101", OrderStatus.Draft,     ("P-004", "Фарба водоемульсійна 10л", 640m, 2)),
        Make("O-004", "C-101", OrderStatus.Confirmed, ("P-005", "Шпаклівка фінішна", 210.75m, 6), ("P-006", "Профіль CD-60", 78.40m, 20)),
        Make("O-005", "C-102", OrderStatus.Cancelled, ("P-007", "Саморізи 3.5x25", 145m, 4)),
        Make("O-006", "C-102", OrderStatus.Draft),
        Make("O-007", "C-102", OrderStatus.Confirmed, ("P-008", "Грунтовка глибокого проникнення", 320m, 3)),
        Make("O-008", "C-103", OrderStatus.Confirmed, ("P-009", "Плитка керамічна", 415.90m, 25), ("P-010", "Клей плитковий", 260m, 5)),
        Make("O-009", "C-103", OrderStatus.Draft,     ("P-010", "Клей плитковий", 260m, 2)),
        Make("O-010", "C-104", OrderStatus.Confirmed, ("P-001", "Цемент М400 25кг", 185.50m, 20)),
        Make("O-011", "C-104", OrderStatus.Cancelled),
        Make("O-012", "C-104", OrderStatus.Draft,     ("P-006", "Профіль CD-60", 78.40m, 40)),
        Make("O-013", "C-105", OrderStatus.Confirmed, ("P-002", "Пісок будівельний", 95m, 10), ("P-003", "Цегла червона", 12.30m, 200)),
        Make("O-014", "C-105", OrderStatus.Draft,     ("P-005", "Шпаклівка фінішна", 210.75m, 1)),
        Make("O-015", "C-100", OrderStatus.Draft,     ("P-008", "Грунтовка глибокого проникнення", 320m, 1)),
        Make("O-016", "C-101", OrderStatus.Confirmed, ("P-007", "Саморізи 3.5x25", 145m, 10)),
    ];

    private static Order Make(string id, string customerId, OrderStatus status,
        params (string ProductId, string Name, decimal Price, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId);

        foreach (var l in lines)
            order.AddLine(l.ProductId, l.Name, l.Price, l.Quantity);

        switch (status)
        {
            case OrderStatus.Confirmed: order.Confirm(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
        }

        return order;
    }
    //lab7:Extra3
    // Детермінована генерація для вимірювань (seed фіксований, дані однакові між запусками)
    public static IReadOnlyList<Order> Generate(int count, int seed = 42)
    {
        var rnd = new Random(seed);
        var result = new List<Order>(count);

        for (int i = 1; i <= count; i++)
        {
            var order = Order.Create($"G-{i:D6}", $"C-{rnd.Next(1, 1001)}");

            int lines = rnd.Next(1, 4);
            for (int j = 0; j < lines; j++)
                order.AddLine($"P-{rnd.Next(1, 200):D3}", "Товар", rnd.Next(10, 1000), rnd.Next(1, 20));

            switch (rnd.Next(3))
            {
                case 0: order.Confirm(); break;
                case 1: order.Cancel(); break;
            }

            result.Add(order);
        }

        return result;
    }
}