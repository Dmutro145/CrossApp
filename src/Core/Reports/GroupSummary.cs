namespace Core.Reports;
public sealed record GroupSummary(string Key, int Count, decimal TotalAmount);
//lab 7 Extra:1
public sealed record CustomerSummary(string CustomerId, string Name, int Orders, decimal TotalAmount);