using System;
using EventStore.Client;
using RedisProjection;
using StackExchange.Redis;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// ---------------- //
// Connect to Redis //
// ---------------- //

var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") 
                ?? "localhost";
var redis = ConnectionMultiplexer.Connect($"{redisHost}:6379")
    .GetDatabase();

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentdbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")           // Get the KurrentDB host from environment variable
                    ?? "localhost";                                      // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                         // Create a connection to KurrentDB
    EventStoreClientSettings.Create(
        $"esdb://admin:changeit@{kurrentdbHost}:2113?tls=false"));

// ------------------------------------------------ //
// Retrieve the last checkpoint position from Redis //
// ------------------------------------------------ //

var checkpointValue = redis.StringGet("checkpoint");                     // Get the checkpoint value from redis
var streamPosition = long.TryParse(checkpointValue, out var checkpoint)  // Check if it exists and convertible to long
    ? FromStream.After(StreamPosition.FromInt64(checkpoint))             // If so, set var to subscribe events from stream after checkpoint
    : FromStream.Start;                                                  // Otherwise, set var to subscribe events from stream from the start

// ---------------------------------------------- //
// Subscribe to KurrentDB from checkpoint onwards //
// ---------------------------------------------- //

await using var subscription = kurrentdb.SubscribeToStream(                   // Subscribe events..
    "$ce-cart",                                                          // from the cart category system projection..        
    streamPosition,                                                      // from this position..
    true);                                                               // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

// ---------------------------------------- //
// Process each event from the subscription //
// ---------------------------------------- //

await foreach (var message in subscription.Messages)                     // Iterate through the messages in the subscription
{                                                                       
    if (message is not StreamMessage.Event(var e)) continue;             // Skip if message is not an event

    var txn = redis.CreateTransaction();                                 // Create a transaction for Redis

    if (!CartProjection.TryProject(txn, e)) continue;                    // Try to project the event by updating a redis key-value pair. If not successful, then skip it

    txn.StringSetAsync("checkpoint", e.OriginalEventNumber.ToInt64());   // Set the checkpoint to the current event number
    
    txn.Execute();                                                       // Execute the transaction. Ensures the projection and checkpoint are successfully written to Redis atomically
}