using System;
using System.Text;
using System.Text.Json;
using EventStore.Client;
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
        "$ce-payment",          // from this stream..
        streamPosition,         // from this position..
        true);                  // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

await foreach (var message in subscription.Messages)                    // Iterate through the messages in the subscription
{                                                                       
    if (message is not StreamMessage.Event(var e)) continue;            // Skip if message is not an event
                                                                        
    var @event = JsonSerializer.Deserialize<PaymentEvent>(              // Deserialize the event
        Encoding.UTF8.GetString(e.Event.Data.Span));                    
                                                                         
    if (@event == null) continue;                                       // Skip if deserialization failed
                                                                                  
    var txn = redis.CreateTransaction();                                // Create a transaction for Redis       
    txn.StringIncrementAsync("payment", (double)@event.amount);         // Update the Redis read model
    txn.StringSetAsync("checkpoint", e.OriginalEventNumber.ToInt64());  // Set the checkpoint to the current event number
    txn.Execute();                                                      // Execute the transaction
                                                                        
    Console.WriteLine($"Incremented redis read model for 'payment'" +   
                                                " by {@event.amount}"); 
}                                                                       

public record PaymentEvent
{
    public decimal? amount { get; set; }
    public DateTime timeStamp { get; set; }
}
