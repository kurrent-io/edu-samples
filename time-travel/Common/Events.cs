namespace Common;

public record OrderPlaced
{
    public string? orderId { get; set; }
    public string? customerId { get; set; }
    public string? checkoutOfCart { get; set; }
    public Store? store { get; set; }
    public List<LineItem>? lineItems { get; set; }
    public ShippingInfo? shipping { get; set; }
    public BillingInfo? billing { get; set; }
    public DateTimeOffset? at { get; set; }

    public record Store
    {
        public string? url { get; set; }
        public string? countryCode { get; set; }
        public string? geographicRegion { get; set; }
    }

    public record LineItem
    {
        public string? productId { get; set; }
        public string? productName { get; set; }
        public string? category { get; set; }
        public int? quantity { get; set; }
        public string? currency { get; set; }
        public decimal? pricePerUnit { get; set; }
        public decimal? taxRate { get; set; }

        public decimal total => (pricePerUnit ?? 0) * (quantity ?? 0); // Calculate the total price for the line item
    }

    public record ShippingInfo
    {
        public RecipientInfo? recipient { get; set; }
        public AddressInfo? address { get; set; }
        public string? instructions { get; set; } = "";
        public string? method { get; set; }
    }

    public record BillingInfo
    {
        public RecipientInfo? recipient { get; set; }
        public AddressInfo? address { get; set; }
        public string? paymentMethod { get; set; }
    }

    public record RecipientInfo
    {
        public string? title { get; set; }
        public string? fullName { get; set; }
        public string? emailAddress { get; set; }
        public string? phoneNumber { get; set; }
    }

    public record AddressInfo
    {
        public string? country { get; set; }
        public List<string>? lines { get; set; }
    }
}