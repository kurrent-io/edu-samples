// =======================================================================================================================
// Report Projection from KurrentDB
// =======================================================================================================================
//
// TODO: Enter description here
// 
// =======================================================================================================================

using EventStore.Client;
using ReportProjection;
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


// ------------------------------- //
// Serialize JSON to report object //
// ------------------------------- //

// TODO: Serialize JSON to report read model object
var report = new ReportReadModel();

// ---------------------------------------------------------- //
// Retrieve the last checkpoint position from JSON Read Model //
// ---------------------------------------------------------- //

// TODO: Retrieve checkpoint from JSON
long? checkpointValue = 0;                                          // Get the checkpoint value from JSON read model
    
var streamPosition = checkpointValue.HasValue                            // Check if the checkpoint exists..
    ? FromStream.After(StreamPosition.FromInt64(checkpointValue.Value))  // if so, subscribe from stream after checkpoint..
    : FromStream.Start;                                                  // otherwise, subscribe from the start of the stream

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(              // Subscribe events..
    "$et-order-placed",                                                  // from the order placed event type system projection..        
    streamPosition,                                                      // from this position..
    true);                                                               // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;             // Skip this message if it is not an event

    if (EventEncoder.Decode(e.Event.Data, "order-placed")                   // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced)                                     // Skip this message if it is not an OrderPlaced event
            continue;
    
    // TODO: update and save report read model based on OrderPlaced event 

    Console.WriteLine($"Projected event " +
                      $"#{e.OriginalEventNumber.ToInt64()} " +
                      $"{e.Event.EventType}");
}