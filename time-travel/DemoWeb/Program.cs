using System.Text.Json;
using EventStore.Client;
using Common;

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentDbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")   // Get the KurrentDB host from environment variable
                    ?? "localhost";                                             // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                           // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://{kurrentDbHost}:2113?tls=false"));

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

var salesDataFilepath = Environment.GetEnvironmentVariable("SALES_DATA_FILEPATH") ?? "data/report.json";


var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/api/sales-data", () =>
{
    if (!File.Exists(salesDataFilepath))
        throw new FileNotFoundException("Sales read model data not found", salesDataFilepath);
    
    var salesDataJson = File.ReadAllText(salesDataFilepath);                // Read the sales report JSON file
    var salesData = JsonSerializer.Deserialize<ReportReadModel>(salesDataJson);   // Deserialize the JSON into a ReportReadModel object
    
    return salesData;
});

app.MapGet("/api/events", async (long checkpoint, DateTimeOffset date, string region, string category, SalesFigureType salesFigureType) =>
{
    var orderEventSummaryList = new List<OrderEventSummaryForSalesReport>(); // Create a list to store OrderPlaced events
    await foreach (var resolvedEvent in kurrentdb.ReadStreamAsync(Direction.Forwards, "$et-order-placed",
                       StreamPosition.Start, resolveLinkTos:true))
    {
        var eventNumber = resolvedEvent.OriginalEventNumber.ToInt64();
        if (eventNumber > checkpoint) break;                    // Stop reading if we reach an event with a number less than or equal to the checkpoint
        
        if (EventEncoder.Decode(resolvedEvent.Event.Data, "order-placed")           // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced)                                         // Skip this message if it is not an OrderPlaced event
            continue;

        if (!OrderMatchesFilter(orderPlaced)) continue; // Skip this message if it does not match the region or category

        orderEventSummaryList.Add(orderPlaced.MapToSummary(eventNumber, category));                                     // Add the event to the list if it matches the date, region, and category
    }

    return orderEventSummaryList;

    bool OrderMatchesFilter(OrderPlaced orderPlaced)
    {
        var orderMatchesRequestedRegion =
            orderPlaced.store!.geographicRegion!.Equals(region, StringComparison.InvariantCultureIgnoreCase);

        var orderMatchesRequestedCategory = orderPlaced.lineItems!.Exists(item =>
            item.category.Equals(category, StringComparison.InvariantCultureIgnoreCase));

        if (!orderMatchesRequestedRegion || !orderMatchesRequestedCategory) return false; // Skip this message if it does not match the region or category

        var orderMatchesRequestedDate = orderPlaced.at!.Value.Date == date.Date;

        var orderIsBeforeRequestedDate = orderPlaced.at!.Value.Date <= date.Date &&
                                         orderPlaced.at!.Value.Year == date.Year &&
                                         orderPlaced.at!.Value.Month == date.Month;

        return (salesFigureType == SalesFigureType.DailySales && orderMatchesRequestedDate) ||
               (salesFigureType == SalesFigureType.TotalMonthlySales && orderIsBeforeRequestedDate);
    }
});

app.UseStaticFiles();

app.MapFallbackToFile("index.html"); // Serve wwwroot/index.html which is built by Vite

app.Run($"http://0.0.0.0:{port}");

public enum SalesFigureType
{
    DailySales,
    TotalMonthlySales
}

public class OrderEventSummaryForSalesReport
{
    public long EventNumber { get; set; }
    public string OrderId { get; set; } = default!;
    public DateTimeOffset At { get; set; }
    public string Region { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string TotalSalesForCategory { get; set; } = default!;
}

public static class Helper
{
    public static OrderEventSummaryForSalesReport MapToSummary(this OrderPlaced orderPlaced, long eventNumber, string category)
    {
        // Find all line items for the given category
        var categoryLineItems = orderPlaced.lineItems!
            .Where(item => item.category.Equals(category, StringComparison.InvariantCultureIgnoreCase));

        // Sum their totals
        var total = categoryLineItems.Sum(item => item.pricePerUnit * item.quantity);

        return new OrderEventSummaryForSalesReport
        {
            EventNumber = eventNumber,
            OrderId = orderPlaced.orderId!,
            At = orderPlaced.at!.Value,
            Region = orderPlaced.store!.geographicRegion!,
            Category = category,
            TotalSalesForCategory = $"USD{total:0.00}"
        };
    }
}



