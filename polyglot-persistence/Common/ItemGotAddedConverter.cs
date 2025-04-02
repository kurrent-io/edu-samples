using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Common;
public class ItemGotAddedConverter : JsonConverter<ItemGotAdded>
{
    private static readonly Regex PricePattern = new Regex(@"^([A-Z]{3})(\d+(?:\.\d+)?)$", RegexOptions.Compiled);

    public override ItemGotAdded Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object");

        var result = new ItemGotAdded();
            
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            var propertyName = reader.GetString();
            reader.Read();
                
            switch (propertyName)
            {
                case "cartId":
                    result.cartId = reader.GetString();
                    break;
                case "productId":
                    result.productId = reader.GetString();
                    break;
                case "productName":
                    result.productName = reader.GetString();
                    break;
                case "quantity":
                    result.quantity = reader.GetInt32();
                    break;
                case "pricePerUnit":
                    var priceString = reader.GetString();
                    if (priceString != null)
                    {
                        var match = PricePattern.Match(priceString);
                        if (match.Success)
                        {
                            result.currency = match.Groups[1].Value;
                            if (decimal.TryParse(match.Groups[2].Value, out var price))
                            {
                                result.pricePerUnit = price;
                            }
                        }
                    }
                    break;
                case "taxRate":
                    result.taxRate = reader.GetDecimal();
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

    public override void Write(Utf8JsonWriter writer, ItemGotAdded value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
            
        writer.WriteString("cartId", value.cartId);
        writer.WriteString("productId", value.productId);
        writer.WriteString("productName", value.productName);
        writer.WriteNumber("quantity", value.quantity);
        writer.WriteString("currency", value.currency);
        writer.WriteNumber("pricePerUnit", value.pricePerUnit);
        writer.WriteNumber("taxRate", value.taxRate);
        writer.WriteString("at", value.at);
            
        writer.WriteEndObject();
    }
}