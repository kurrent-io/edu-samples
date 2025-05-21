namespace DemoWeb;

public enum SalesFigureType
{
    DailySales,
    TotalMonthlySales
}

public class OrderEventSummary
{
    public long EventNumber { get; set; }
    public string OrderId { get; set; } = default!;
    public DateTimeOffset At { get; set; }
    public string Region { get; set; } = default!;
    public string Category { get; set; } = default!;
    public decimal TotalSalesForCategory { get; set; } = default!;
}