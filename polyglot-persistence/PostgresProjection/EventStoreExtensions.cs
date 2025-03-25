using CaseConverter;
using EventStore.Client;
using System.Text.Json;
using System.Text;

namespace PostgresProjection
{
    public static class EventStoreExtensions
    {
        public static Event ToEvent(this ResolvedEvent resolvedEvent)
        {
            var eventType = GetEventType(resolvedEvent.Event.EventType);
            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

            Console.WriteLine($"Processing event: {eventType}");

            if (eventType == null || JsonSerializer.Deserialize(eventData, eventType) is not Event e)
                throw new Exception($"Failed to deserialize event: {resolvedEvent.Event.EventType}");

            return e;
        }
        
        public static Type? GetEventType(string eventType)
        {
            return typeof(Event).Assembly.GetTypes().FirstOrDefault(t =>
                t.Name.Equals(eventType.ToPascalCase(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
