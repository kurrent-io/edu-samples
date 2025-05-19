using Common;

namespace DemoWeb;

public static class Helper
{
    public static OrderEventSummary MapToSummary(this OrderPlaced orderPlaced, long eventNumber, string category)
    {
        // Find all line items for the given category
        var categoryLineItems = orderPlaced.lineItems!.Where(item =>
            item.category != null && item.category.Equals(category, StringComparison.InvariantCultureIgnoreCase));

        // Sum their totals
        var total = categoryLineItems.Sum(item => item.total);

        return new OrderEventSummary
        {
            EventNumber = eventNumber,
            OrderId = orderPlaced.orderId!,
            At = orderPlaced.at!.Value,
            Region = orderPlaced.store!.geographicRegion!,
            Category = category,
            TotalSalesForCategory = total
        };
    }
}