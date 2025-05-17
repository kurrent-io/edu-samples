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
using ReportProjection;
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

    var d = orderPlaced.at!.Value;
    var lastDayInMonth = DateTime.DaysInMonth(d.Year, d.Month);
    //var reportDate = new DateTimeOffset(d.Year, d.Month, lastDayInMonth, 0, 0, 0, d.Offset);
    var reportDateInString = d.ToString("yyyy-MM-dd");

    var region = orderPlaced.store!.geographicRegion!;                                    // Get the region from the OrderPlaced event

    if (!readModel.SalesReports.ContainsKey(reportDateInString))                // Check if the report for the order date already exists
        readModel.SalesReports[reportDateInString] = Helper.Create(d.Year, d.Month, Config.GetCategories(), Config.GetRegions(), Config.GetTargetMonthlySales(d.Year, d.Month));         // If not, create a new sales report for the order date

    var salesReport = readModel.SalesReports[reportDateInString];

    foreach (var lineItem in orderPlaced.lineItems!)
    {
        var category = lineItem.category!;

        var regionSalesReport = salesReport.CategorySalesReports[category].RegionSalesReports[region];
        var currentDailySales = lineItem.pricePerUnit!.Value * lineItem.quantity!.Value;              // Add the revenue (price * quantity) to the daily sales total in the report

        regionSalesReport.DailySales += currentDailySales; // Update the daily sales for the region in the category sales report
        
        for (var i = d.Day; i <= lastDayInMonth; i++) // For each day in the month so far
        {
            var dateInThePast = new DateTimeOffset(d.Year, d.Month, i, 0, 0, 0, d.Offset);
            var dateInStringInThePast = dateInThePast.ToString("yyyy-MM-dd");

            if (!readModel.SalesReports.ContainsKey(dateInStringInThePast))                // Check if the report for the order date already exists
                readModel.SalesReports[dateInStringInThePast] = Helper.Create(d.Year, d.Month, Config.GetCategories(), Config.GetRegions(), Config.GetTargetMonthlySales(d.Year, d.Month));         // If not, create a new sales report for the order date

            var salesReportInThePast = readModel.SalesReports[dateInStringInThePast];
            
            var regionSalesReportInThePast = salesReportInThePast.CategorySalesReports[category].RegionSalesReports[region];

            regionSalesReportInThePast.TotalMonthlySales += currentDailySales; // Update the daily sales for the region in the category sales report
            regionSalesReportInThePast.TargetHitRate = regionSalesReportInThePast.TargetSales == 0 ? 0 : Math.Round(regionSalesReportInThePast.TotalMonthlySales / regionSalesReportInThePast.TargetSales, 2); // Update the target hit rate for the region in the category sales report
        }
    }

    readModel.Checkpoint = e.OriginalEventNumber.ToInt64();                                       // Set the read model checkpoint to the event number of the event we just read

    Directory.CreateDirectory(Directory.GetParent(readModelPath)!.FullName);                    // Create the directory for the report read model if it doesn't exist
    File.WriteAllText(readModelPath, JsonSerializer.Serialize(readModel));              // Write the report read model to the JSON file
    
    Console.WriteLine($"Projected event " +
                      $"#{e.OriginalEventNumber.ToInt64()} " +
                      $"{e.Event.EventType}");
}