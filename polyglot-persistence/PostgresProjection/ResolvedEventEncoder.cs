using System.Text;
using System.Text.Json;
using EventStore.Client;

namespace PostgresProjection
{
    public static class ResolvedEventEncoder
    {
        private static readonly Dictionary<string, Type> EventTypeMap = new()
        {
            { "visitor-started-shopping", typeof(VisitorStartedShopping) },
            { "customer-started-shopping", typeof(CustomerStartedShopping) },
            { "cart-shopper-got-identified", typeof(CartShopperGotIdentified) },
            { "item-got-added-to-cart", typeof(ItemGotAdded) },
            { "item-got-removed-from-cart", typeof(ItemGotRemoved) },
            { "cart-got-checked-out", typeof(CartGotCheckedOut) },
            { "cart-got-abandoned", typeof(CartGotAbandoned) }
        };

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Converters = { new ItemGotAddedConverter() }
        };
        
        public static object? DecodeEvent(this ResolvedEvent resolvedEvent)
        {
            var eventType = GetEventType(resolvedEvent.Event.EventType);
            if (eventType == null)
            {
                Console.WriteLine($"Unknown event type: {resolvedEvent.Event.EventType}");
                return null;
            }

            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
            Console.WriteLine($"Processing event: {eventType}");

            try
            {
                return JsonSerializer.Deserialize(eventData, eventType, SerializerOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize event: {resolvedEvent.Event.EventType}. Error: {ex.Message}");
                return null;
            }
        }

        public static Type? GetEventType(string eventType)
        {
            EventTypeMap.TryGetValue(eventType, out var type);
            return type;
        }
    }
}