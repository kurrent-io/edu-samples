using System.Text.Json;
using EventStore.Client;
using Common;
using DemoWeb;

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

// ----------------------------------- //
// Define the sales-data API endpoints //
// ----------------------------------- //

app.MapGet("/api/sales-data", () =>
{
    var salesDataFilepath =                                                 // Get the path to the sales data file 
        Environment.GetEnvironmentVariable("SALES_DATA_FILEPATH") ??        // from an environment variable
        "data/report.json";                                                 // or default to "data/report.json"

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
        "$et-order-placed", StreamPosition.Start, resolveLinkTos:true);     // from the start of the $et-order-placed stream

    await foreach (var resolvedEvent in readResults)                        // For each event in the stream
    {
        var eventNumber = resolvedEvent.OriginalEventNumber.ToInt64();      // Get its event number from the stream
        if (eventNumber > checkpoint) break;                                // Stop reading if the event number is greater than the checkpoint

        if (EventEncoder.Decode(resolvedEvent.Event.Data, "order-placed")   // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced)                                 // Skip this message if it is not an OrderPlaced event
            continue;

        if (!OrderMatchesFilter(orderPlaced)) continue;                     // Skip this message if it does not filter from the request

        orderEventSummaryList.Add(                                          // Otherwise, add the order event to the list
            orderPlaced.MapToSummary(eventNumber, category));               // after mapping it to a summary object
    }

    return orderEventSummaryList;

    bool OrderMatchesFilter(OrderPlaced orderPlaced)
    {
        var matchRegion =                                                   // Check if the order matches the requested region
            orderPlaced.store!.geographicRegion!.
                Equals(region, 
                    StringComparison.InvariantCultureIgnoreCase);           // Ignore case for region comparison

        var matchCategory = orderPlaced.lineItems!.Exists(item =>           // Check if any line item matches the requested category
            item.category.Equals(category, 
                StringComparison.InvariantCultureIgnoreCase));              // Ignore case for category comparison

        if (!matchRegion || !matchCategory) return false;                   // Skip this event if it does not match the region or category

        var matchDate = orderPlaced.at!.Value.Date == date.Date;            // Check if the order date matches the requested date

        var orderIsBeforeRequestedDate =                                    // Check if the order date is before the requested date
            orderPlaced.at!.Value.Date <= date.Date &&
            orderPlaced.at!.Value.Year == date.Year &&
            orderPlaced.at!.Value.Month == date.Month;

        if (salesFigureType == SalesFigureType.DailySales)                  // If the sales figure type is daily sales
            return matchDate;                                               // return true if the order date matches the requested date

        if (salesFigureType == SalesFigureType.TotalMonthlySales)           // If the sales figure type is total monthly sales
            return orderIsBeforeRequestedDate;                              // return true if the order date is before the requested date

        throw new ArgumentOutOfRangeException(                              // Otherwise, throw an exception
            nameof(salesFigureType), salesFigureType, null);
    }
});

// -------------------- //
// Start the web server //
// -------------------- //

app.UseStaticFiles();

app.MapFallbackToFile("index.html"); // Serve wwwroot/index.html which is built by Vite

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

app.Run($"http://0.0.0.0:{port}");