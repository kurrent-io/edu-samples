using System.Text;
using System.Text.Json;
using EventStore.Client;
using MongoDB.Driver;
using MongoDB.Bson;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// Connect to MongoDB
var mongoHost = Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
var mongoCollection = new MongoClient($"mongodb://{mongoHost}:27017").GetDatabase("polyglot-persistence").GetCollection<BsonDocument>("total-payment");

// Connect to KurrentDB
var esdbHost = Environment.GetEnvironmentVariable("ESDB_HOST") ?? "localhost";
var esdb = new EventStoreClient(EventStoreClientSettings.Create($"esdb://admin:changeit@{esdbHost}:2113?tls=false"));

var checkpointValue = mongoCollection                      // Get the checkpoint value from MongoDB..
  .Find(Builders<BsonDocument>.Filter.Eq("_id", "total"))  // from the total document's.. 
  .FirstOrDefault()?["checkpoint"]?.AsInt64;               // checkpoint field

var streamPosition = checkpointValue.HasValue                            // Check if the checkpoint exists..
    ? FromStream.After(StreamPosition.FromInt64(checkpointValue.Value))  // If so, subscribe from stream after checkpoint..
    : FromStream.Start;                                                  // Otherwise, subscribe from the start of the stream

await using var subscription = 
    esdb.SubscribeToStream(     // Subscribe events..
        "$ce-payment",          // from this stream..
        streamPosition,         // from this position..
        true);                  // with linked events automatically resolved (required for system projections)

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

await foreach (var message in subscription.Messages)          // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;  // Skip if message is not an event
    
    var @event = JsonSerializer.Deserialize<PaymentEvent>(    // Deserialize the event
        Encoding.UTF8.GetString(e.Event.Data.Span));
     
    if (@event == null) continue;                             // Skip if deserialization failed
          
    var updateCommand = Builders<BsonDocument>.Update         // Create command to update both the read model and checkpoint in a single operation
        .Inc("total", (double)(@event.amount ?? 0))
        .Set("checkpoint", e.OriginalEventNumber.ToInt64());
    
    mongoCollection.UpdateOne(                                // Update the mongo..
      new BsonDocument("_id", "total"),                       // for the total document..
      updateCommand,                                          // with the update command..
      new UpdateOptions { IsUpsert = true });                 // and create it if it doesn't exist
    
    Console.WriteLine($"Updated MongoDB document 'total'. " +
                     $"Incremented total by {@event.amount}, " +
                     $"checkpoint set to {e.OriginalEventNumber.ToInt64()}");
}

public record PaymentEvent
{
    public decimal? amount { get; set; }
    public DateTime timeStamp { get; set; }
}