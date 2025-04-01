using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CaseConverter;
using EventStore.Client;

namespace RedisProjection
{
    public static class EventStoreExtensions
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

        public static Event ToEvent(this ResolvedEvent resolvedEvent)
        {
            var eventType = GetEventType(resolvedEvent.Event.EventType);
            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

            Console.WriteLine($"Processing event: {eventType}");

            if (eventType == null || JsonSerializer.Deserialize(eventData, eventType, SerializerOptions) is not Event e)
                throw new Exception($"Failed to deserialize event: {resolvedEvent.Event.EventType}");

            return e;
        }

        public static Type? GetEventType(string eventType)
        {
            EventTypeMap.TryGetValue(eventType, out var type);
            return type;
        }
    }
}