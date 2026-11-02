using Core.Reports;

namespace Core.Tests;

/// <summary>Звіти тижня 7 — чисті функції: вхід — список, вихід — DTO, жодних залежностей.</summary>
public class ReportServiceTests
{
    // 5) звіт на кількох наборах: умова «дорожче за поріг» строга
    [Theory]
    [InlineData(0, 4)]
    [InlineData(100, 3)]
    [InlineData(5000, 1)]
    [InlineData(10000, 0)]
    public void ExpensiveOrders_CountsOnlyAboveThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.ExpensiveOrders(TestData.Orders(), threshold).Count);

    [Fact]   // 6) детермінований топ-N: O-MID і O-TIE мають однакову суму, перемагає менший id
    public void TopOrders_IsDeterministic() =>
        Assert.Equal(new[] { "O-BIG", "O-MID" },
            ReportService.TopOrders(TestData.Orders(), 2).Select(row => row.Id));

    [Fact]
    public void Totals_CountOnlyConfirmedOrders()
    {
        var totals = ReportService.Totals(TestData.Orders());

        Assert.Equal((3, 20000m, 20000m / 3, 10000m), totals);
    }

    [Fact]
    public void Totals_OnEmptySource_ReturnsZeros() =>
        Assert.Equal((0, 0m, 0m, 0m), ReportService.Totals([]));
}
