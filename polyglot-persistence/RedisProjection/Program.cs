using System;
using System.Text;
using System.Text.Json;
using EventStore.Client;
using RedisProjection;
using StackExchange.Redis;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

var redis = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();                                          // Connect to Redis
var esdb = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false")); // Connect to EventStoreDB

var checkpointValue = redis.StringGet("checkpoint");                     // Get the checkpoint value from redis
var streamPosition = long.TryParse(checkpointValue, out var checkpoint)  // Check if it exists and convertible to long
    ? FromStream.After(StreamPosition.FromInt64(checkpoint))             // If so, set var to subscribe events from stream after checkpoint
    : FromStream.Start;                                                  // Otherwise, set var to subscribe events from stream from the start

await using var subscription = 
    esdb.SubscribeToStream(     // Subscribe events..
        "$et-item-got-added-to-cart",          // from this stream..
        streamPosition,         // from this position..
        true);                  // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

await foreach (var message in subscription.Messages)                    // Iterate through the messages in the subscription
{                                                                       
    if (message is not StreamMessage.Event(var e)) continue;            // Skip if message is not an event

    var evt = e.ToEvent() as ItemGotAdded;

    if (evt == null) continue;   // Convert the event to ItemGotAdded

    var hourKey = $"top-10-products:{evt.at:yyyyMMddHH}";     // Create a key for the current hour
    var productKey = evt.productName;                             // Use the product ID as the member in the sorted set
    var txn = redis.CreateTransaction();                        // Create a transaction for Redis
    txn.SortedSetIncrementAsync(hourKey, productKey, evt.quantity); // Increment the quantity of the product in the sorted set
    txn.StringSetAsync("checkpoint", e.OriginalEventNumber.ToInt64()); // Set the checkpoint to the current event number
    txn.Execute();

    Console.WriteLine($"Incremented product {evt.productId} in {hourKey} by {evt.quantity}");
}                                                                       