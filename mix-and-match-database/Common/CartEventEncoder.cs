using System.Text;
using System.Text.Json;

namespace Common;

public static class CartEventEncoder
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
        
    public static object? Decode(ReadOnlyMemory<byte> eventData, string eventTypeName)
    {
        if (!EventTypeMap.TryGetValue(eventTypeName, out var eventType))
        {
            Console.WriteLine($"Unknown event type: {eventTypeName}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(Encoding.UTF8.GetString(eventData.Span), eventType, SerializerOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize event: {eventTypeName}. Error: {ex.Message}");
            return null;
        }
    }
}