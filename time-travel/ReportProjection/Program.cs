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

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentDbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST") // Get the KurrentDB host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                    // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://{kurrentDbHost}:2113?tls=false"));

// --------------------------------- //
// Deserialize JSON to report object //
// --------------------------------- //
var readModelPath =                                                      // Get the path to the report read model from an environment variable
    Environment.GetEnvironmentVariable("OUTPUT_FILEPATH") ?? 
    "data/report-read-model.json"; 

var hasExistingReadModel = File.Exists(readModelPath);

var readModel = LoadOrCreateReadModel(readModelPath);                    // Deserialize the JSON file into a ReportReadModel object

// ---------------------------------------------------------- //
// Retrieve the last checkpoint position from JSON Read Model //
// ---------------------------------------------------------- //

var streamPosition = hasExistingReadModel                                // Check if the checkpoint exists..
    ? FromStream.After(StreamPosition.FromInt64(readModel.Checkpoint))   // if so, subscribe from stream after checkpoint..
    : FromStream.Start;                                                  // otherwise, subscribe from the start of the stream

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(              // Subscribe to events..
    "$et-order-placed",                                                  // from the order placed event type system projection..        
    streamPosition,                                                      // from this position..
    true);                                                               // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing to events from stream after {streamPosition}");

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;             // Skip this message if it is not an event

    if (EventEncoder.Decode(e.Event.Data, "order-placed")                // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced) continue;                    // Skip this message if it is not an OrderPlaced event

    ReportProjection.ReportProjection.ProjectOrderToReadModel(           // Project the event to the read model
        orderPlaced, readModel);

    readModel.Checkpoint = e.OriginalEventNumber.ToInt64();              // Set the read model checkpoint to the event number of the event we just read

    SaveReadModel(readModel, readModelPath);                             // Save the read model to the JSON file

    Console.WriteLine($"Projected event " +
                      $"#{e.OriginalEventNumber.ToInt64()} " +
                      $"{e.Event.EventType}");
}

ReportReadModel LoadOrCreateReadModel(string readModelPath)
{
    return File.Exists(readModelPath)                                    // Check if the read model file exists
        ? JsonSerializer.Deserialize<ReportReadModel>(                   // If it does, deserialize it
              File.ReadAllText(readModelPath))                           // By reading the file contents and deserialize to ReportReadModel
          ?? new ReportReadModel()                                       // If file is not found or deserialization fails
        : new ReportReadModel();                                         // create a new empty ReportReadModel object 
}

void SaveReadModel(ReportReadModel readModel, string readModelPath)
{
    Directory.CreateDirectory(                                           // Create the directory for the report read model if it doesn't exist
        Directory.GetParent(readModelPath)!.FullName);

    File.WriteAllText(readModelPath,                                     // Write the report read model to the JSON file
        JsonSerializer.Serialize(readModel,                              // serialize the read model to JSON
            new JsonSerializerOptions { WriteIndented = true }));        // with indentation for readability
    ;
}