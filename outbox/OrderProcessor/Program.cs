// =======================================================================================================================
// Order Processor with KurrentDB
// =======================================================================================================================
// This sample demonstrates how to process OrderPlaced events from KurrentDB and update a fulfillment table in Postgres.
//
// It:
// 
// 1. Connects to KurrentDB and Postgres databases
// 2. Creates a subscription to the "$ce-order" stream in KurrentDB
// 3. Listens for incoming order events through the subscription
// 4. Processes OrderPlaced events and records them in the fulfillment table in Postgres
// 5. Implements error handling for both transient and permanent database errors
// 6. Acknowledges events appropriately based on processing outcome
// 
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

var kurrentdbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")        // Get the KurrentDB host from environment variable
                    ?? "localhost";                                             // Default to localhost if not set

var kurrentdb = new EventStorePersistentSubscriptionsClient(                    // Create a connection to KurrentDB
                        EventStoreClientSettings.Create(
                            $"esdb://{kurrentdbHost}:2113?tls=false"));

// ------------------------------- //
// Connect and initialize Postgres //
// ------------------------------- //

var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST")          // Get the Postgres host from environment variable
                    ?? "localhost";                                             // Default to localhost if not set

var postgres = new PostgresDataAccess(                                          // Create a postgres connection and inject into a custom data access class
                    new NpgsqlConnection(
                        $"Host={postgresHost};Port=5432;" +
                        $"Database=postgres;Username=postgres"));

// ---------------------------------------------- //
// Subscribe to the $ce-order stream in KurrentDB //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(                     // Subscribe to the $ce-order stream in KurrentDB
		"$ce-order",
		"fulfillment");

Console.WriteLine("Subscribing events from stream");

var repository = new OrderFulfillmentRepository(postgres);                      // Create an instance of the repository to insert order fulfillment to postgres

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                            // Iterate through the messages in the subscription
{
    if (message is PersistentSubscriptionMessage.NotFound)                      // Skip this message if the subscription is not found
    {
        Console.WriteLine("Persistent subscription consumer group not found." +
            "Please recreate it.");
        continue;
    }

    if (message is not PersistentSubscriptionMessage.Event(var e, _))           // Skip this message if it is not an event 
            continue;                                                   

    try
    {
        Console.WriteLine($"Received event #{e.Link!.EventNumber} in " +         // Log the event number of the event in the $ce-order stream
                          $"{e.Link.EventStreamId} stream");             
        if (EventEncoder.Decode(e.Event.Data, "order-placed")                   // Try to deserialize the event to an OrderPlaced event
            is not OrderPlaced orderPlaced)                                     // Skip this message if it is not an OrderPlaced event
            continue;

        repository.StartOrderFulfillment(orderPlaced.orderId);                  // Process the OrderPlaced event by inserting an order fulfillment record into Postgres

        await subscription.Ack(e);                                              // Send an acknowledge message to the consumer group so that it will send the next event
    }
    catch (Exception ex)
    {
        // ------------------------------------------------------------- //
        // Warning: This is just one example of a transient error check; //
        //          You should to add more checks based on your needs    //
        // ------------------------------------------------------------- //
        var exceptionIsTransient =                                              // Exception is transient if it matches one of the following patterns:
            ex is SocketException ||                                            // SocketException indicating a network error (https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketexception?view=dotnet-plat-ext-7.0)    
            ex is NpgsqlException { IsTransient: true };                        // Postgres exception indicating the error is transient (https://www.npgsql.org/doc/api/Npgsql.NpgsqlException.html#Npgsql_NpgsqlException_IsTransient)

        if (exceptionIsTransient)                                               // If exception is transient..
        {
            Console.WriteLine($"Detected DB transient error {ex.Message}. Retrying.");
            await subscription.Nack(PersistentSubscriptionNakEventAction.Retry, // Send a not acknowledge message to the consumer group and request it to retry
                "Detected DB transient error", e);
            Thread.Sleep(1000);                                                 // Wait for a second before retrying to avoid overwhelming the database
        }
        else                                                                    // If exception is not transient (i.e. permanent)..
        {
            Console.WriteLine($"Detected permanent error {ex.Message}. Skipping.");
            await subscription.Nack(PersistentSubscriptionNakEventAction.Skip, // Send a not acknowledge message to the consumer group and request it to skip
                "Detected permanent error", e);
        }
    }
}