namespace Core.Reports;

public sealed record OrderLineReport(string OrderId, string CustomerId, string Status, decimal Total);