// ========================================================================================================
// PostgreSQL Projection from EventStoreDB
// ========================================================================================================
// This sample demonstrates how to project events from EventStoreDB to a read model in PostgreSQL.
//
// It:
// 
// 1. Connects to PostgreSQL and EventStoreDB
// 2. Retrieves the last checkpoint position from PostgreSQL
// 3. Subscribes to the cart category projection stream in EventStoreDB
// 4. Processes each event to update the PostgreSQL read model
// 5. Maintains a checkpoint in PostgreSQL to track progress
// 
// This creates a current state of the cart optimized for queries while
// maintaining event sourcing in EventStoreDB as the source of truth.
// ========================================================================================================

using EventStore.Client;
using Npgsql;
using PostgresProjection;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// -------------------------------------- //
// Connect to PostgreSQL and EventStoreDB //
// -------------------------------------- //

var postgres = new PostgresDataAccess(new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=postgres"));
var esdb = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));

// ------------------------------------------------- //
// Subscribe to EventStoreDB from checkpoint onwards //
// ------------------------------------------------- //

var checkpointValue = postgres.GetCheckpoint("carts");                   // Get the checkpoint value from PostgreSQL checkpoint table

var streamPosition = checkpointValue.HasValue                            // Check if the checkpoint exists..
    ? FromStream.After(StreamPosition.FromInt64(checkpointValue.Value))  // if so, subscribe from stream after checkpoint..
    : FromStream.Start;                                                  // otherwise, subscribe from the start of the stream  

await using var subscription = esdb.SubscribeToStream(                   // Subscribe events..
    "$ce-cart",                                                          // from the cart category system projection..        
    streamPosition,                                                      // from this position..
    true);                                                               // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

// ------------------------------------------------------------- //
// Handle events and update PostgreSQL read model and checkpoint //
// ------------------------------------------------------------- //

var eventHandler = new CartEventHandler(postgres);                       // Initialize event handler
await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;             // Skip if message is not an event
    
    postgres.BeginTransaction();                                         // Begin transaction to ensure checkpoint and read model are updated atomically

    eventHandler.UpdatePostgresReadModel(e);                             // Update the PostgreSQL read model according to the event
    postgres.UpdateCheckpoint("carts", e.OriginalEventNumber.ToInt64()); // Update the checkpoint value in PostgreSQL checkpoint table

    postgres.Commit();                                                   // Commit transaction
}