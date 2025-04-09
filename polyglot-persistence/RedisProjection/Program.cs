using System;
using Common;
using EventStore.Client;
using StackExchange.Redis;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// Program.cs
var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var redis = ConnectionMultiplexer.Connect($"{redisHost}:6379").GetDatabase();    
                                      // Connect to Redis
var esdbHost = Environment.GetEnvironmentVariable("ESDB_HOST") ?? "localhost";
var esdb = new EventStoreClient(EventStoreClientSettings.Create($"esdb://admin:changeit@{esdbHost}:2113?tls=false")); // Connect to EventStoreDB

var checkpointValue = redis.StringGet("checkpoint");                     // Get the checkpoint value from redis
var streamPosition = long.TryParse(checkpointValue, out var checkpoint)  // Check if it exists and convertible to long
    ? FromStream.After(StreamPosition.FromInt64(checkpoint))             // If so, set var to subscribe events from stream after checkpoint
    : FromStream.Start;                                                  // Otherwise, set var to subscribe events from stream from the start

await using var subscription = 
    esdb.SubscribeToStream(     // Subscribe events..
        "$ce-cart",          // from this stream..
        streamPosition,         // from this position..
        true);                  // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

await foreach (var message in subscription.Messages)                    // Iterate through the messages in the subscription
{                                                                       
    if (message is not StreamMessage.Event(var e)) continue;            // Skip if message is not an event

    var decodedEvent = CartEventEncoder.Decode(e.Event.Data, e.Event.EventType);
    if (decodedEvent is not ItemGotAdded && decodedEvent is not ItemGotRemoved) continue;   // Convert the event to ItemGotAdded

    var txn = redis.CreateTransaction();                        // Create a transaction for Redis
    if (decodedEvent is ItemGotAdded addedEvent) // If the event is of type ItemGotAdded
    {
        var hourKey = $"top-10-products:{addedEvent.at:yyyyMMddHH}";     // Create a key for the current hour
        var productKey = addedEvent.productId;                             // Use the product ID as the member in the sorted set
        var productName = addedEvent.productName; // Assuming `productName` is part of the event

        txn.SortedSetIncrementAsync(hourKey, productKey, addedEvent.quantity); // Increment the quantity of the product in the sorted set
        txn.HashSetAsync("product-names", productKey, productName); // Store product name in a hash
        Console.WriteLine($"Incremented product {addedEvent.productId} in {hourKey} by {addedEvent.quantity}");
    }
    else if (decodedEvent is ItemGotRemoved removedEvent) // If the event is of type ItemGotRemoved
    {
        var hourKey = $"top-10-products:{removedEvent.at:yyyyMMddHH}"; // Create a key for the current hour
        var productKey = removedEvent.productId; // Use the product ID as the member in the sorted set
        
        txn.SortedSetDecrementAsync(hourKey, productKey, removedEvent.quantity); // Increment the quantity of the product in the sorted set
        Console.WriteLine($"Decremented product {removedEvent.productId} in {hourKey} by {removedEvent.quantity}");
    }

    txn.StringSetAsync("checkpoint", e.OriginalEventNumber.ToInt64()); // Set the checkpoint to the current event number
    txn.Execute();

    
}                                                                       