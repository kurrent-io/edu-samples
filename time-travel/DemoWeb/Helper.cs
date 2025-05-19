using Common;

namespace DemoWeb;

public static class Helper
{
    public static OrderEventSummaryForSalesReport MapToSummary(this OrderPlaced orderPlaced, long eventNumber, string category)
    {
        // Find all line items for the given category
        var categoryLineItems = orderPlaced.lineItems!.Where(item =>
            item.category != null && item.category.Equals(category, StringComparison.InvariantCultureIgnoreCase));

        // Sum their totals
        var total = categoryLineItems.Sum(item => item.pricePerUnit * item.quantity);

        return new OrderEventSummaryForSalesReport
        {
            EventNumber = eventNumber,
            OrderId = orderPlaced.orderId!,
            At = orderPlaced.at!.Value,
            Region = orderPlaced.store!.geographicRegion!,
            Category = category,
            TotalSalesForCategory = total ?? 0
        };
    }
}