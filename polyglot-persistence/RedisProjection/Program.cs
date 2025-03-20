using System;
using System.Text;
using System.Text.Json;
using EventStore.Client;
using StackExchange.Redis;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

var redis = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();
var esdb = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));

var streamPosition = long.TryParse(redis.StringGet("checkpoint"), out var checkpoint)
    ? FromStream.After(StreamPosition.FromInt64(checkpoint))
    : FromStream.Start;

await using var subscription = esdb.SubscribeToStream("$ce-payment", streamPosition, true);

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

await foreach (var message in subscription.Messages)
{
    if (message is not StreamMessage.Event(var e)) continue;
    
    var @event = JsonSerializer.Deserialize<PaymentEvent>(Encoding.UTF8.GetString(e.Event.Data.Span));
    
    if (@event == null) continue;

    var redisTransaction = redis.CreateTransaction();
    redisTransaction.StringIncrementAsync("payment", (double)@event.amount);
    redisTransaction.StringSetAsync("checkpoint", e.OriginalEventNumber.ToInt64());
    redisTransaction.Execute();

    Console.WriteLine($"Incremented redis value for 'payment' by {@event.amount}");
}

public record PaymentEvent
{
    public decimal? amount { get; set; }
    public DateTime timeStamp { get; set; }
}
