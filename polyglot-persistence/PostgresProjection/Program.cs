using System.Text;
using System.Text.Json;
using EventStore.Client;
using Npgsql;
using StreamPosition = EventStore.Client.StreamPosition;

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// Connect to PostgreSQL
var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=postgres");
conn.Open();

// Connect to EventStoreDB
var esdb = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));

// Create read model table if it doesn't exist already
new NpgsqlCommand(@"
    CREATE TABLE IF NOT EXISTS total_payments (
        id TEXT PRIMARY KEY,
        total DECIMAL NOT NULL DEFAULT 0,
        checkpoint BIGINT NULL
    )", conn).ExecuteNonQuery();

// Get the checkpoint value from PostgreSQL
long? checkpointValue = null;
var result = new NpgsqlCommand("SELECT checkpoint FROM total_payments WHERE id = 'payment'", conn).ExecuteScalar();
if (result != null && result != DBNull.Value) 
    checkpointValue = Convert.ToInt64(result);

var streamPosition = checkpointValue.HasValue                            // Check if the checkpoint exists..
    ? FromStream.After(StreamPosition.FromInt64(checkpointValue.Value))  // If so, set var to subscribe events from stream after checkpoint..
    : FromStream.Start;                                                  // Otherwise, set var to subscribe events from the start

await using var subscription = esdb.SubscribeToStream(  // Subscribe events..
                                "$ce-payment",          // from this stream..
                                streamPosition,         // from this position..
                                true);                  // with linked events automatically resolved    

Console.WriteLine($"Subscribing events from stream after {streamPosition}");

await foreach (var message in subscription.Messages)          // Iterate through the messages in the subscription
{
    if (message is not StreamMessage.Event(var e)) continue;  // Skip if message is not an event
    
    var @event = JsonSerializer.Deserialize<PaymentEvent>(    // Deserialize the event
        Encoding.UTF8.GetString(e.Event.Data.Span));
     
    if (@event == null) continue;                             // Skip if deserialization failed

    // Update payment total and checkpoint within a single transaction
    var cmd = new NpgsqlCommand(@"
        INSERT INTO total_payments (id, total, checkpoint)
        VALUES ('payment', @amount, @checkpoint)
        ON CONFLICT (id) DO UPDATE 
        SET total = total_payments.total + @amount,
            checkpoint = @checkpoint", conn);
    
    cmd.Parameters.AddWithValue("amount", (decimal)(@event.amount ?? 0));
    cmd.Parameters.AddWithValue("checkpoint", e.OriginalEventNumber.ToInt64());
    cmd.ExecuteNonQuery();
    
    Console.WriteLine($"Updated PostgreSQL table 'total_payments'. " +
                      $"Incremented total by {@event.amount}, " +
                      $"checkpoint set to {e.OriginalEventNumber.ToInt64()}");
}

conn.Close();

public record PaymentEvent
{
    public decimal? amount { get; set; }
    public DateTime timeStamp { get; set; }
}