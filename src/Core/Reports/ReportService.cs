using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1) фільтр + сортування + проєкція: замовлення дорожче порога
    public static IReadOnlyList<OrderLineReport> BigOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
              .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
              .Select(o => new OrderLineReport(o.Id, o.CustomerId, o.Status.ToString(), o.Total))
              .ToList();

    // 2) топ-N зі стабільним порядком
    public static IReadOnlyList<OrderLineReport> TopByTotal(IEnumerable<Order> source, int n) =>
        source.OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
              .Take(n)
              .Select(o => new OrderLineReport(o.Id, o.CustomerId, o.Status.ToString(), o.Total))
              .ToList();

    public static IReadOnlyList<GroupSummary> TopCustomers(IEnumerable<Order> source, int n) =>
        source.GroupBy(o => o.CustomerId, StringComparer.Ordinal)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .OrderByDescending(s => s.TotalAmount).ThenBy(s => s.Key, StringComparer.Ordinal)
              .Take(n)
              .ToList();

    // 3) групування за статусом з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .OrderByDescending(s => s.TotalAmount)
              .ToList();

    // 4) агрегати з обробкою порожньої послідовності
    public static (int Items, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0m, 0m)
            : (list.Count, list.Sum(o => o.Total),
               list.Average(o => o.Total), list.Max(o => o.Total));
    }
    // Навмисно IEnumerable, а не List: для демо відкладеного виконання запит не має виконуватися одразу
    public static IEnumerable<Order> CheapOrdersLazy(IEnumerable<Order> source, decimal threshold) =>
       source.Where(o => o.Total < threshold);
}