using Common;

namespace DemoWeb;

public static class Helper
{
    public static OrderEventSummary MapToSummary(this OrderPlaced orderPlaced, long eventNumber, string category)
    {
        // Find all line items for the given category
        var categoryLineItems = orderPlaced.LineItems!.Where(item =>
            item.Category != null && item.Category.Equals(category, StringComparison.InvariantCultureIgnoreCase));

        // Sum their totals
        var total = categoryLineItems.Sum(item => item.Total);

        return new OrderEventSummary
        {
            EventNumber = eventNumber,
            OrderId = orderPlaced.OrderId!,
            At = orderPlaced.At!.Value,
            Region = orderPlaced.Store!.GeographicRegion!,
            Category = category,
            TotalSalesForCategory = total
        };
    }
}