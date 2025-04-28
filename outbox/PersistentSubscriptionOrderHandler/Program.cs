// =======================================================================================================================
// Postgres Projection from KurrentDB
// =======================================================================================================================
// This sample demonstrates how to project events from KurrentDB to a read model in Postgres.
//
// It:
// 
// 1. Connects to Postgres and KurrentDB
// 2. Retrieves the last checkpoint position from Postgres
// 3. Subscribes to the cart category projection stream in KurrentDB
// 4. Iterates each event from the subscription
// 5. Processes each event to update the Postgres read model
// 6. Maintains a checkpoint in Postgres to track progress
// 
// The read model is a denormalized view of the cart events, which can be used for reporting or querying purposes.
// The projection is done in a way that ensures the read model is always up to date with the latest events from KurrentDB.
// =======================================================================================================================

using Common;
using EventStore.Client;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentdbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")           // Get the KurrentDB host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var kurrentdb = new EventStorePersistentSubscriptionsClient(                                         // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://admin:changeit@{kurrentdbHost}:2113?tls=false"));

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(
		"$et-order-placed",
		"fulfillment-group");

Console.WriteLine("Subscribing events from stream");

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{
    if (message is not PersistentSubscriptionMessage.Event(var e, _)) continue;             // Skip this message if it is not an event

    if (EventEncoder.Decode(e.Event.Data, "order-placed") is not OrderPlaced orderPlaced) continue;

    Console.WriteLine($"Projected event " +
                      $"#{e.OriginalEventNumber.ToInt64()} " +
                      $"{e.Event.EventType}");

    Console.WriteLine($"Order ID: {orderPlaced.orderId}");

    await subscription.Ack(e); // Acknowledge the event to mark it as processed
}