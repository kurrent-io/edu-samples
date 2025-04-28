using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Common;

public class OrderPlacedConverter : JsonConverter<OrderPlaced>
{
    private static readonly Regex PricePattern = new Regex(@"^([A-Z]{3})(\d+(?:\.\d+)?)$", RegexOptions.Compiled);

    public override OrderPlaced Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object");

        var result = new OrderPlaced
        {
            lineItems = new List<OrderPlaced.LineItem>()
        };

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "orderId":
                    result.orderId = reader.GetString() ?? throw new JsonException("orderId cannot be null");
                    break;
                case "customerId":
                    result.customerId = reader.GetString() ?? throw new JsonException("customerId cannot be null");
                    break;
                case "checkoutOfCart":
                    result.checkoutOfCart = reader.GetString() ?? throw new JsonException("checkoutOfCart cannot be null");
                    break;
                case "lineItems":
                    result.lineItems = ReadLineItems(ref reader);
                    break;
                case "shipping":
                    result.shipping = ReadShippingInfo(ref reader);
                    break;
                case "billing":
                    result.billing = ReadBillingInfo(ref reader);
                    break;
                case "at":
                    result.at = reader.GetDateTimeOffset();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return result;
    }

    private List<OrderPlaced.LineItem> ReadLineItems(ref Utf8JsonReader reader)
    {
        var lineItems = new List<OrderPlaced.LineItem>();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for lineItems");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object for lineItem");

            var lineItem = new OrderPlaced.LineItem();
            
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name in lineItem");

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "productId":
                        lineItem.productId = reader.GetString() ?? throw new JsonException("productId cannot be null");
                        break;
                    case "productName":
                        lineItem.productName = reader.GetString() ?? throw new JsonException("productName cannot be null");
                        break;
                    case "quantity":
                        lineItem.quantity = reader.GetInt32();
                        break;
                    case "pricePerUnit":
                        var priceString = reader.GetString();
                        if (priceString != null)
                        {
                            var match = PricePattern.Match(priceString);
                            if (match.Success)
                            {
                                lineItem.currency = match.Groups[1].Value;
                                if (decimal.TryParse(match.Groups[2].Value, out var price))
                                {
                                    lineItem.pricePerUnit = price;
                                }
                            }
                        }
                        break;
                    case "taxRate":
                        lineItem.taxRate = reader.GetDecimal();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            
            lineItems.Add(lineItem);
        }

        return lineItems;
    }

    private OrderPlaced.ShippingInfo ReadShippingInfo(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for shipping");

        var shipping = new OrderPlaced.ShippingInfo
        {
            recipient = new OrderPlaced.RecipientInfo(),
            address = new OrderPlaced.AddressInfo { lines = new List<string>() }
        };

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name in shipping");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "recipient":
                    shipping.recipient = ReadRecipientInfo(ref reader);
                    break;
                case "address":
                    shipping.address = ReadAddressInfo(ref reader);
                    break;
                case "instructions":
                    shipping.instructions = reader.GetString() ?? "";
                    break;
                case "method":
                    shipping.method = reader.GetString() ?? throw new JsonException("shipping method cannot be null");
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return shipping;
    }

    private OrderPlaced.BillingInfo ReadBillingInfo(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for billing");

        var billing = new OrderPlaced.BillingInfo
        {
            recipient = new OrderPlaced.RecipientInfo(),
            address = new OrderPlaced.AddressInfo { lines = new List<string>() }
        };

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name in billing");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "recipient":
                    billing.recipient = ReadRecipientInfo(ref reader);
                    break;
                case "address":
                    billing.address = ReadAddressInfo(ref reader);
                    break;
                case "paymentMethod":
                    billing.paymentMethod = reader.GetString() ?? throw new JsonException("payment method cannot be null");
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return billing;
    }

    private OrderPlaced.RecipientInfo ReadRecipientInfo(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for recipient");

        var recipient = new OrderPlaced.RecipientInfo();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name in recipient");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "title":
                    recipient.title = reader.GetString() ?? throw new JsonException("title cannot be null");
                    break;
                case "fullName":
                    recipient.fullName = reader.GetString() ?? throw new JsonException("fullName cannot be null");
                    break;
                case "emailAddress":
                    recipient.emailAddress = reader.GetString() ?? throw new JsonException("emailAddress cannot be null");
                    break;
                case "phoneNumber":
                    recipient.phoneNumber = reader.GetString() ?? throw new JsonException("phoneNumber cannot be null");
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return recipient;
    }

    private OrderPlaced.AddressInfo ReadAddressInfo(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for address");

        var address = new OrderPlaced.AddressInfo
        {
            lines = new List<string>()
        };

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name in address");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "country":
                    address.country = reader.GetString() ?? throw new JsonException("country cannot be null");
                    break;
                case "lines":
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected start of array for address lines");

                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        address.lines.Add(reader.GetString() ?? "");
                    }
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return address;
    }

    public override void Write(Utf8JsonWriter writer, OrderPlaced value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("orderId", value.orderId);
        writer.WriteString("customerId", value.customerId);
        writer.WriteString("checkoutOfCart", value.checkoutOfCart);

        writer.WritePropertyName("lineItems");
        writer.WriteStartArray();
        foreach (var item in value.lineItems)
        {
            writer.WriteStartObject();
            writer.WriteString("productId", item.productId);
            writer.WriteString("productName", item.productName);
            writer.WriteNumber("quantity", item.quantity);
            
            // Combine currency and price for serialization
            if (!string.IsNullOrEmpty(item.currency))
            {
                writer.WriteString("pricePerUnit", $"{item.currency}{item.pricePerUnit}");
            }
            else
            {
                writer.WriteNumber("pricePerUnit", item.pricePerUnit);
            }
            
            writer.WriteNumber("taxRate", item.taxRate);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        WriteShippingInfo(writer, value.shipping);
        WriteBillingInfo(writer, value.billing);

        writer.WriteString("at", value.at);

        writer.WriteEndObject();
    }

    private void WriteShippingInfo(Utf8JsonWriter writer, OrderPlaced.ShippingInfo shipping)
    {
        writer.WritePropertyName("shipping");
        writer.WriteStartObject();
        
        WriteRecipientInfo(writer, shipping.recipient);
        WriteAddressInfo(writer, shipping.address);
        
        writer.WriteString("instructions", shipping.instructions);
        writer.WriteString("method", shipping.method);
        
        writer.WriteEndObject();
    }

    private void WriteBillingInfo(Utf8JsonWriter writer, OrderPlaced.BillingInfo billing)
    {
        writer.WritePropertyName("billing");
        writer.WriteStartObject();
        
        WriteRecipientInfo(writer, billing.recipient);
        WriteAddressInfo(writer, billing.address);
        
        writer.WriteString("paymentMethod", billing.paymentMethod);
        
        writer.WriteEndObject();
    }

    private void WriteRecipientInfo(Utf8JsonWriter writer, OrderPlaced.RecipientInfo recipient)
    {
        writer.WritePropertyName("recipient");
        writer.WriteStartObject();
        
        writer.WriteString("title", recipient.title);
        writer.WriteString("fullName", recipient.fullName);
        writer.WriteString("emailAddress", recipient.emailAddress);
        writer.WriteString("phoneNumber", recipient.phoneNumber);
        
        writer.WriteEndObject();
    }

    private void WriteAddressInfo(Utf8JsonWriter writer, OrderPlaced.AddressInfo address)
    {
        writer.WritePropertyName("address");
        writer.WriteStartObject();
        
        writer.WriteString("country", address.country);
        
        writer.WritePropertyName("lines");
        writer.WriteStartArray();
        foreach (var line in address.lines)
        {
            writer.WriteStringValue(line);
        }
        writer.WriteEndArray();
        
        writer.WriteEndObject();
    }
}
