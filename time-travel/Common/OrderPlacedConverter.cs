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
            LineItems = new List<OrderPlaced.LineItem>()
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
                    result.OrderId = reader.GetString() ?? throw new JsonException("orderId cannot be null");
                    break;
                case "customerId":
                    result.CustomerId = reader.GetString() ?? throw new JsonException("customerId cannot be null");
                    break;
                case "checkoutOfCart":
                    result.CheckoutOfCart = reader.GetString() ?? throw new JsonException("checkoutOfCart cannot be null");
                    break;
                case "lineItems":
                    result.LineItems = ReadLineItems(ref reader);
                    break;
                case "store":
                    result.Store = ReadStore(ref reader);
                    break;
                case "shipping":
                    result.Shipping = ReadShippingInfo(ref reader);
                    break;
                case "billing":
                    result.Billing = ReadBillingInfo(ref reader);
                    break;
                case "at":
                    result.At = reader.GetDateTimeOffset();
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
                        lineItem.ProductId = reader.GetString() ?? throw new JsonException("productId cannot be null");
                        break;
                    case "productName":
                        lineItem.ProductName = reader.GetString() ?? throw new JsonException("productName cannot be null");
                        break;
                    case "category":
                        lineItem.Category = reader.GetString();
                        break;
                    case "quantity":
                        lineItem.Quantity = reader.GetInt32();
                        break;
                    case "pricePerUnit":
                        var priceString = reader.GetString();
                        if (priceString != null)
                        {
                            var match = PricePattern.Match(priceString);
                            if (match.Success)
                            {
                                lineItem.Currency = match.Groups[1].Value;
                                if (decimal.TryParse(match.Groups[2].Value, out var price))
                                {
                                    lineItem.PricePerUnit = price;
                                }
                            }
                        }
                        break;
                    case "taxRate":
                        lineItem.TaxRate = reader.GetDecimal();
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

    private OrderPlaced.StoreInfo ReadStore(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for store");

        var store = new OrderPlaced.StoreInfo();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name in store");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "url":
                    store.Url = reader.GetString() ?? throw new JsonException("store url cannot be null");
                    break;
                case "countryCode":
                    store.CountryCode = reader.GetString() ?? throw new JsonException("store countryCode cannot be null");
                    break;
                case "geographicRegion":
                    store.GeographicRegion = reader.GetString() ?? throw new JsonException("store geographicRegion cannot be null");
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return store;
    }

    private OrderPlaced.ShippingInfo ReadShippingInfo(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for shipping");

        var shipping = new OrderPlaced.ShippingInfo
        {
            Recipient = new OrderPlaced.RecipientInfo(),
            Address = new OrderPlaced.AddressInfo { Lines = new List<string>() }
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
                    shipping.Recipient = ReadRecipientInfo(ref reader);
                    break;
                case "address":
                    shipping.Address = ReadAddressInfo(ref reader);
                    break;
                case "instructions":
                    shipping.Instructions = reader.GetString() ?? "";
                    break;
                case "method":
                    shipping.Method = reader.GetString() ?? throw new JsonException("shipping method cannot be null");
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
            Recipient = new OrderPlaced.RecipientInfo(),
            Address = new OrderPlaced.AddressInfo { Lines = new List<string>() }
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
                    billing.Recipient = ReadRecipientInfo(ref reader);
                    break;
                case "address":
                    billing.Address = ReadAddressInfo(ref reader);
                    break;
                case "paymentMethod":
                    billing.PaymentMethod = reader.GetString() ?? throw new JsonException("payment method cannot be null");
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
                    recipient.Title = reader.GetString() ?? throw new JsonException("title cannot be null");
                    break;
                case "fullName":
                    recipient.FullName = reader.GetString() ?? throw new JsonException("fullName cannot be null");
                    break;
                case "emailAddress":
                    recipient.EmailAddress = reader.GetString() ?? throw new JsonException("emailAddress cannot be null");
                    break;
                case "phoneNumber":
                    recipient.PhoneNumber = reader.GetString() ?? throw new JsonException("phoneNumber cannot be null");
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
            Lines = new List<string>()
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
                    address.Country = reader.GetString() ?? throw new JsonException("country cannot be null");
                    break;
                case "lines":
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected start of array for address lines");

                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        address.Lines.Add(reader.GetString() ?? "");
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

        writer.WriteString("orderId", value.OrderId);
        writer.WriteString("customerId", value.CustomerId);
        writer.WriteString("checkoutOfCart", value.CheckoutOfCart);

        writer.WritePropertyName("lineItems");
        writer.WriteStartArray();
        foreach (var item in value.LineItems!)
        {
            writer.WriteStartObject();
            writer.WriteString("productId", item.ProductId);
            writer.WriteString("productName", item.ProductName);
            writer.WriteString("category", item.Category);
            writer.WriteNumber("quantity", item.Quantity!.Value);
            
            // Combine currency and price for serialization
            if (!string.IsNullOrEmpty(item.Currency))
            {
                writer.WriteString("pricePerUnit", $"{item.Currency}{item.PricePerUnit}");
            }
            else
            {
                writer.WriteNumber("pricePerUnit", item.PricePerUnit!.Value);
            }
            
            writer.WriteNumber("taxRate", item.TaxRate!.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        WriteStore(writer, value.Store!);
        WriteShippingInfo(writer, value.Shipping!);
        WriteBillingInfo(writer, value.Billing!);

        writer.WriteString("at", value.At!.Value);

        writer.WriteEndObject();
    }

    private void WriteStore(Utf8JsonWriter writer, OrderPlaced.StoreInfo store)
    {
        writer.WritePropertyName("store");
        writer.WriteStartObject();
        
        writer.WriteString("url", store.Url);
        writer.WriteString("countryCode", store.CountryCode);
        writer.WriteString("geographicRegion", store.GeographicRegion);
        
        writer.WriteEndObject();
    }

    private void WriteShippingInfo(Utf8JsonWriter writer, OrderPlaced.ShippingInfo shipping)
    {
        writer.WritePropertyName("shipping");
        writer.WriteStartObject();
        
        WriteRecipientInfo(writer, shipping.Recipient!);
        WriteAddressInfo(writer, shipping.Address!);
        
        writer.WriteString("instructions", shipping.Instructions);
        writer.WriteString("method", shipping.Method);
        
        writer.WriteEndObject();
    }

    private void WriteBillingInfo(Utf8JsonWriter writer, OrderPlaced.BillingInfo billing)
    {
        writer.WritePropertyName("billing");
        writer.WriteStartObject();
        
        WriteRecipientInfo(writer, billing.Recipient!);
        WriteAddressInfo(writer, billing.Address!);
        
        writer.WriteString("paymentMethod", billing.PaymentMethod);
        
        writer.WriteEndObject();
    }

    private void WriteRecipientInfo(Utf8JsonWriter writer, OrderPlaced.RecipientInfo recipient)
    {
        writer.WritePropertyName("recipient");
        writer.WriteStartObject();
        
        writer.WriteString("title", recipient.Title);
        writer.WriteString("fullName", recipient.FullName);
        writer.WriteString("emailAddress", recipient.EmailAddress);
        writer.WriteString("phoneNumber", recipient.PhoneNumber);
        
        writer.WriteEndObject();
    }

    private void WriteAddressInfo(Utf8JsonWriter writer, OrderPlaced.AddressInfo address)
    {
        writer.WritePropertyName("address");
        writer.WriteStartObject();
        
        writer.WriteString("country", address.Country);
        
        writer.WritePropertyName("lines");
        writer.WriteStartArray();
        foreach (var line in address.Lines!)
        {
            writer.WriteStringValue(line);
        }
        writer.WriteEndArray();
        
        writer.WriteEndObject();
    }
}
