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

using EventStore.Client;
using Npgsql;
using PostgresProjection;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// ------------------------------- //
// Connect and initialize Postgres //
// ------------------------------- //

var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST")   // Get the Postgres host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var postgres = new PostgresDataAccess(                                   // Create a postgres connection and inject into a custom data access class
                    new NpgsqlConnection(
                        $"Host={postgresHost};Port=5432;" +
                        $"Database=postgres;Username=postgres"));

postgres.Execute(CartProjection.GetCreateTableCommand());                // Create the cart related tables if it doesn't exist

postgres.Execute("CREATE TABLE IF NOT EXISTS checkpoints " +             // Create the checkpoint table if it doesn't exist
                 "(read_model_name TEXT PRIMARY KEY," +
                 "checkpoint BIGINT NOT NULL)");

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentDbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST") // Get the KurrentDB host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                    // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://admin:changeit@{kurrentDbHost}:2113?tls=false"));

// --------------------------------------------------- //
// Retrieve the last checkpoint position from Postgres //
// --------------------------------------------------- //

var checkpointValue = postgres.QueryFirstOrDefault<long?>(               // Get the checkpoint value from Postgres checkpoint table
    "SELECT checkpoint " +
    "FROM checkpoints " +
    "WHERE read_model_name = 'carts'");                  

var streamPosition = checkpointValue.HasValue                            // Check if the checkpoint exists..
    ? FromStream.After(StreamPosition.FromInt64(checkpointValue.Value))  // if so, subscribe from stream after checkpoint..
    : FromStream.Start;                                                  // otherwise, subscribe from the start of the stream

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(              // Subscribe events..
    "$ce-cart",                                                          // from the cart category system projection..        
    streamPosition,                                                      // from this position..
    true);                                                               // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;             // Skip this message if it is not an event

    postgres.BeginTransaction();                                         // Begin a transaction for Postgres
    
    postgres.Execute(CartProjection.Project(e));                         // Update the Postgres read model based on the event being processed
    
    postgres.Execute(                                                    
        "INSERT INTO checkpoints (read_model_name, checkpoint) " +       // Insert checkpoint into the checkpoint table 
        "VALUES (@ReadModelName, @Checkpoint) " +
        "ON CONFLICT (read_model_name) DO " +                            // If the read model name already exists..
        "UPDATE SET checkpoint = @Checkpoint",                           // then update the checkpoint value
        new
        {
            ReadModelName = "carts", 
            Checkpoint = e.OriginalEventNumber.ToInt64()                 // Get the stream position from the event
        });

    postgres.Commit();                                                   // Commit the transaction only if the read model and checkpoint are updated successfully

    Console.WriteLine($"Projected event " +
                      $"#{e.OriginalEventNumber.ToInt64()} " +
                      $"{e.Event.EventType}");
}