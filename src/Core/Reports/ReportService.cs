using Core.Domain;
using Core.Dto;
namespace Core.Reports;

public static class ReportService
{
    // 1) фільтр + сортування + проєкція: замовлення дорожче порога
    public static IReadOnlyList<OrderLineReport> BigOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
              .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
              .Select(o => new OrderLineReport(o.Id, o.CustomerId, o.Status.ToString(), o.Total))
              .ToList();

    //lab7:Extra2
    // 2) топ-N зі стабільним порядком (через TopBy)
    public static IReadOnlyList<OrderLineReport> TopByTotal(IEnumerable<Order> source, int n) =>
        source.TopBy(o => o.Total, o => o.Id, n)
              .Select(o => new OrderLineReport(o.Id, o.CustomerId, o.Status.ToString(), o.Total))
              .ToList();

    public static IReadOnlyList<GroupSummary> TopCustomers(IEnumerable<Order> source, int n) =>
        source.GroupBy(o => o.CustomerId, StringComparer.Ordinal)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .TopBy(s => s.TotalAmount, s => s.Key, n)
              .ToList();

    // 3) групування за статусом з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .OrderByDescending(s => s.TotalAmount)
              .ToList();
    //lab7:Extra3
    public static IReadOnlyList<GroupSummary> ByStatusQuery(IEnumerable<Order> source)
    {
        var query =
            from o in source
            group o by o.Status.ToString() into g
            let total = g.Sum(x => x.Total)
            orderby total descending
            select new GroupSummary(g.Key, g.Count(), total);

        return query.ToList();
    }

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
       
      // 5) GroupJoin: клієнти × замовлення, зведений DTO (клієнти без замовлень теж потрапляють)
    public static IReadOnlyList<CustomerSummary> CustomerTotals(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
        customers.GroupJoin(orders,
                    c => c.Id,
                    o => o.CustomerId,
                    (c, os) => new CustomerSummary(c.Id, c.Name, os.Count(), os.Sum(o => o.Total)))
                .OrderByDescending(s => s.TotalAmount).ThenBy(s => s.CustomerId, StringComparer.Ordinal)
                .ToList(); 

    // Варіант A: один ланцюжок, одна матеріалізація в кінці
    public static IReadOnlyList<GroupSummary> TopConfirmedCustomers(IEnumerable<Order> source, int n) =>
        source.Where(o => o.Status == OrderStatus.Confirmed)
              .GroupBy(o => o.CustomerId, StringComparer.Ordinal)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .TopBy(s => s.TotalAmount, s => s.Key, n)
              .ToList();

    // Варіант B: ToList() після кожного кроку (зайві проміжні колекції)
    public static IReadOnlyList<GroupSummary> TopConfirmedCustomersEager(IEnumerable<Order> source, int n)
    {
        var confirmed = source.Where(o => o.Status == OrderStatus.Confirmed).ToList();
        var groups = confirmed.GroupBy(o => o.CustomerId, StringComparer.Ordinal).ToList();
        var summaries = groups.Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total))).ToList();
        return summaries.TopBy(s => s.TotalAmount, s => s.Key, n).ToList();
    }
}

public static class EnumerableExtensions
{
    // топ-N за спаданням ключа зі стабільним порядком (другий ключ обов'язковий)
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source, Func<T, TKey> key, Func<T, string> tieBreaker, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        return source.OrderByDescending(key)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}