// =======================================================================================================================
// Report Projection from KurrentDB
// =======================================================================================================================
//
// This projection takes Sales events in KurrentDB and projects them into a Report JSON object.
//
// It:
//
// 1. Connects to KurrentDB
// 2. Looks at the latest Report JSON file to find the current checkpoint
// 3. Creates a subscription to the "$et-order-placed" stream in KurrentDB
// 4. Updates the relevant sales report fields in the ReportReadModel object in response to order-placed events
// 5. Updates the checkpoint in the ReportReadModel object 
// 5. Serializes the ReportReadModel object into JSON and overwrites the latest report file
// =======================================================================================================================

using System.Text.Json;
using EventStore.Client;
using Common;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

var readModelPath = Environment.GetEnvironmentVariable("OUTPUT_FILEPATH") ?? "data/report.json"; // Get the path to the report read model from an environment variable

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentDbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")   // Get the KurrentDB host from environment variable
                    ?? "localhost";                                             // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                           // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://{kurrentDbHost}:2113?tls=false"));


// --------------------------------- //
// Deserialize JSON to report object //
// --------------------------------- //

var hasExistingReadModel = File.Exists(readModelPath);

var readModel = hasExistingReadModel                                                                                           // Check whether there is an existing read model JSON file...
    ? JsonSerializer.Deserialize<ReportReadModel>(File.ReadAllText(readModelPath)) ?? new ReportReadModel()               // if so, shen deserialize the report JSON into a record
    : new ReportReadModel();                                                                                                   // otherwise, initialize a new ReportReadModel

// ---------------------------------------------------------- //
// Retrieve the last checkpoint position from JSON Read Model //
// ---------------------------------------------------------- //

var checkpointValue = readModel.Checkpoint;                                     // Get the checkpoint value from the report read model object
    
var streamPosition = hasExistingReadModel
    ? FromStream.After(StreamPosition.FromInt64(checkpointValue))
    : FromStream.Start;

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(              // Subscribe to events..
    "$et-order-placed",                                                  // from the order placed event type system projection..        
    streamPosition,                                                      // from this position..
    true);                                                   // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing to events from stream after {streamPosition}");

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                                            // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;                        // Skip this message if it is not an event

    if (EventEncoder.Decode(e.Event.Data, "order-placed")                          // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced)                                                     // Skip this message if it is not an OrderPlaced event
            continue;

    var orderDate = orderPlaced.at!.Value.Date.ToString("yyyy-MM-dd");                   // Get the date part of the "at" timestamp in the OrderPlaced event

    var region = orderPlaced.store!.geographicRegion!;                                    // Get the region from the OrderPlaced event
    
    foreach (var lineItem in orderPlaced.lineItems!)
    {
        var category = lineItem.category!;
        
        var salesReport = readModel.SalesReports.GetValueOrDefault(orderDate, new SalesReport());                           // Get the sales report for the order date
        var categorySalesData = salesReport.CategorySalesReports.GetValueOrDefault(category, new CategorySalesReport());    // Get the report for the category within the daily sales report
        var regionSalesData = categorySalesData.RegionSalesReports.GetValueOrDefault(region, new RegionSalesReport());      // Get the report for the region within the category sales report
        var previousDay = GetPreviousDayInMonth(orderDate);

        var previousRegionalSalesData = readModel.SalesReports!
            .GetValueOrDefault(previousDay)?.CategorySalesReports
            .GetValueOrDefault(category)?.RegionSalesReports
            .GetValueOrDefault(region);
        
        var currentDailySales = regionSalesData.DailySales + lineItem.pricePerUnit!.Value * lineItem.quantity!.Value;              // Add the revenue (price * quantity) to the daily sales total in the report

        var newSalesData = regionSalesData with
        {
            DailySales = currentDailySales,
            TotalMonthlySales = previousRegionalSalesData is null ? currentDailySales : previousRegionalSalesData.TotalMonthlySales + currentDailySales
        };
        
        categorySalesData.RegionSalesReports[region] = newSalesData;                                                        // Update the read model object with the new 
        salesReport.CategorySalesReports[category] = categorySalesData;
        readModel.SalesReports[orderDate] = salesReport;
    }

    readModel.Checkpoint = e.OriginalEventNumber.ToInt64();                                       // Set the read model checkpoint to the event number of the event we just read

    Directory.CreateDirectory(Directory.GetParent(readModelPath)!.FullName);                    // Create the directory for the report read model if it doesn't exist
    File.WriteAllText(readModelPath, JsonSerializer.Serialize(readModel));              // Write the report read model to the JSON file
    
    Console.WriteLine($"Projected event " +
                      $"#{e.OriginalEventNumber.ToInt64()} " +
                      $"{e.Event.EventType}");
}

string? GetPreviousDayInMonth(string dateString)
{
    var date = DateTime.Parse(dateString);
    
    return date.Day == 1 ? null : date.AddDays(-1).ToString("yyyy-MM-dd");
}