using System;
using System.Text;
using System.Text.Json;
using EventStore.Client;
using StackExchange.Redis;
using StreamPosition = EventStore.Client.StreamPosition;

var redis = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();
var esdb = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));

var streamPosition = long.TryParse(redis.StringGet("checkpoint"), out var checkpoint)
    ? FromStream.After(StreamPosition.FromInt64(checkpoint))
    : FromStream.Start;

await using var subscription = esdb.SubscribeToStream("$ce-payment", streamPosition, true);

await foreach (var message in subscription.Messages)
{
    if (message is not StreamMessage.Event(var evnt)) continue;
    
    var paymentEvent = JsonSerializer.Deserialize<PaymentEvent>(Encoding.UTF8.GetString(evnt.Event.Data.Span));
    
    if (paymentEvent == null) continue;
    
    Console.WriteLine($"Processing payment: {paymentEvent.id}, Amount: {paymentEvent.amount} {paymentEvent.currency}");

    var redisTransaction = redis.CreateTransaction();
    redisTransaction.StringIncrementAsync("payment", (double)(paymentEvent.amount ?? 0));
    redisTransaction.StringSetAsync("checkpoint", evnt.OriginalEventNumber.ToInt64());
    redisTransaction.ExecuteAsync();
}

public record PaymentEvent
{
    public string? id { get; set; }
    public decimal? amount { get; set; }
    public string? currency { get; set; }
    public DateTime timeStamp { get; set; }
}
