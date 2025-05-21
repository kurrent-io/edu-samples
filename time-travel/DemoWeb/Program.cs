using System.Text.Json;
using EventStore.Client;
using Common;
using DemoWeb;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentDbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")    // Get the KurrentDB host from environment variable
                    ?? "localhost";                                         // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                       // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://{kurrentDbHost}:2113?tls=false"));

// -------------------- //
// Setup the web server //
// -------------------- //

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

// ----------------------------------- //
// Define the sales-data API endpoints //
// ----------------------------------- //

app.MapGet("/api/sales-data", () =>
{
    var salesDataFilepath =                                                 // Get the path to the sales data file 
        Environment.GetEnvironmentVariable("SALES_DATA_FILEPATH") ??        // from an environment variable
        "data/report-read-model.json";                                      // or default to "data/report-read-model.json"

    if (!File.Exists(salesDataFilepath))                                    // If the file does not exist
        throw new FileNotFoundException("Sales read model data not found",  // throw an exception
            salesDataFilepath);
    
    var salesDataJson = File.ReadAllText(salesDataFilepath);                // Read the sales report JSON file
    var salesData = JsonSerializer.                                         // Deserialize the JSON into a ReportReadModel object
        Deserialize<ReportReadModel>(salesDataJson);   
    
    return salesData;
});

// ------------------------------ //
// Define the event API endpoints //
// ------------------------------ //

app.MapGet("/api/events", async (long checkpoint, DateTimeOffset date, 
    string region, string category, SalesFigureType salesFigureType) =>
{
    var orderEventSummaryList = new List<OrderEventSummary>();              // Create a list to hold filtered order events

    var readResults = kurrentdb.ReadStreamAsync(Direction.Forwards,         // Read the stream in the forward direction
        "$et-order-placed", StreamPosition.Start, resolveLinkTos:true,      // from the start of the $et-order-placed stream
        maxCount: checkpoint + 1);                                          // up to the checkpoint + 1 (note: checkpoint is zero-based)

    await foreach (var resolvedEvent in readResults)                        // For each event in the stream
    {
        if (EventEncoder.Decode(resolvedEvent.Event.Data, "order-placed")   // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced) continue;                       // Skip this message if it is not an OrderPlaced event

        if (OrderDoesNotMatchRegionOrCategory(orderPlaced)) continue;       // Skip if the order does not match the requested region and category

        switch (salesFigureType)
        {
            case SalesFigureType.DailySales:                                // If the sales figure type is daily sales
                if (OrderDoesNotMatchRequestDate(orderPlaced)) continue;    // Skip if the order was not placed on the report date
                break;
            case SalesFigureType.TotalMonthlySales:                         // If the sales figure type is total monthly sales
                if (OrderIsPlacedAfterRequestDate(orderPlaced)) continue;   // Skip if the order was placed after the report date
                break;
            default:
                throw new ArgumentOutOfRangeException();                    // If the sales figure type is not recognized, throw an exception
        }

        var eventNumber = resolvedEvent.OriginalEventNumber.ToInt64();      // Get its event number from the stream

        orderEventSummaryList.Add(                                          // Otherwise, add the order event to the list
            orderPlaced.MapToSummary(eventNumber, category));               // after mapping it to a summary object
    }

    return orderEventSummaryList.OrderByDescending(x => x.EventNumber)      // Order the list by event number in descending order
        .ToList();                                                          // and convert it to a list

    bool OrderDoesNotMatchRegionOrCategory(OrderPlaced orderPlaced)
    {
        var matchRegion =                                                   // Check if the order matches the requested region
            orderPlaced.Store!.GeographicRegion!.
                Equals(region,
                    StringComparison.InvariantCultureIgnoreCase);           // Ignore case for region comparison

        var matchCategory = orderPlaced.LineItems!.Exists(item =>           // Check if any line item matches the requested category
            item != null && item.Category.Equals(category,
                StringComparison.InvariantCultureIgnoreCase));              // Ignore case for category comparison

        return !(matchRegion && matchCategory);                             // Check if order matches both region and category
    }

    bool OrderDoesNotMatchRequestDate(OrderPlaced orderPlaced)
    {
        return orderPlaced.At!.Value.Date != date.Date;                     // Check if the order date matches the requested date
        
    }

    bool OrderIsPlacedAfterRequestDate(OrderPlaced orderPlaced)
    {
        return orderPlaced.At!.Value.Date > date.Date ||                    // Check if the order is placed after the requested date
               orderPlaced.At!.Value.Year != date.Year ||
               orderPlaced.At!.Value.Month != date.Month;
        
    }

});

// -------------------- //
// Start the web server //
// -------------------- //

app.UseStaticFiles();

app.MapFallbackToFile("index.html"); // Serve wwwroot/index.html which is built by Vite

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

app.Run($"http://0.0.0.0:{port}");