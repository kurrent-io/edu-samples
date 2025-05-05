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

using System.Net.Sockets;
using Common;
using EventStore.Client;
using Npgsql;
using OrderProcessor;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentdbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")           // Get the KurrentDB host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var kurrentdb = new EventStorePersistentSubscriptionsClient(                                         // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://admin:changeit@{kurrentdbHost}:2113?tls=false"));

// ------------------------------- //
// Connect and initialize Postgres //
// ------------------------------- //

var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST")   // Get the Postgres host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var postgres = new PostgresDataAccess(                                   // Create a postgres connection and inject into a custom data access class
                    new NpgsqlConnection(
                        $"Host={postgresHost};Port=5432;" +
                        $"Database=postgres;Username=postgres"));

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(
		"$ce-order",
		"fulfillment-group");

Console.WriteLine("Subscribing events from stream");

var repository = new OrderFulfillmentRepository(postgres);

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{
    if (message is PersistentSubscriptionMessage.NotFound)
    {
        Console.WriteLine("Persistent subscription consumer group not found. Please recreate it.");
        continue;
    }

    if (message is not PersistentSubscriptionMessage.Event(var e, var retryCount)) continue;             // Skip this message if it is not an event

    try
    {
        if (EventEncoder.Decode(e.Event.Data, "order-placed") is not OrderPlaced orderPlaced) continue;

        repository.StartOrderFulfillment(orderPlaced.orderId);

        await subscription.Ack(e); // Acknowledge the event to mark it as processed
    }
    catch (Exception ex)
    {
        // Warning: This is just one example of a transient error check; you should to add more checks based on your needs
        var exceptionIsTransient = ex is SocketException { SocketErrorCode: SocketError.TryAgain } ||
                                   ex is NpgsqlException { IsTransient: true };

        if (exceptionIsTransient)
        {
            Console.WriteLine($"Detected DB transient error {ex}. Retrying.");
            await subscription.Nack(PersistentSubscriptionNakEventAction.Retry, "Detected DB transient error", e);
            Thread.Sleep(1000);
        }
        else
        {
            Console.WriteLine($"Detected permanent error {ex}. Skipping.");
            await subscription.Nack(PersistentSubscriptionNakEventAction.Skip, "Detected permanent error", e);
        }
    }
}