using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory] // звіт на кількох наборах
    [InlineData(500, 2)]
    [InlineData(1000, 1)]
    [InlineData(5000, 0)]
    public void BigOrders_CountsOnlyAboveThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.BigOrders(TestData.Items(), threshold).Count);

    [Fact] // детермінований топ-N
    public void TopByTotal_IsDeterministic() =>
        Assert.Equal(new[] { "O-BIG", "O-MID" },
            ReportService.TopByTotal(TestData.Items(), 2).Select(r => r.OrderId));

    [Fact] // однакові суми: порядок за Id
    public void TopByTotal_WithEqualTotals_SortsById()
    {
        var items = new[]
        {
            TestData.Make("B-2", "C-1", 100m, 1),
            TestData.Make("A-1", "C-1", 100m, 1)
        };

        Assert.Equal(new[] { "A-1", "B-2" },
            ReportService.TopByTotal(items, 2).Select(r => r.OrderId));
    }
}