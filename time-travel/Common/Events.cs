namespace Common;

public record OrderPlaced
{
    public string? OrderId { get; set; }
    public string? CustomerId { get; set; }
    public string? CheckoutOfCart { get; set; }
    public StoreInfo? Store { get; set; }
    public List<LineItem>? LineItems { get; set; }
    public ShippingInfo? Shipping { get; set; }
    public BillingInfo? Billing { get; set; }
    public DateTimeOffset? At { get; set; }

    public record StoreInfo
    {
        public string? Url { get; set; }
        public string? CountryCode { get; set; }
        public string? GeographicRegion { get; set; }
    }

    public record LineItem
    {
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public int? Quantity { get; set; }
        public string? Currency { get; set; }
        public decimal? PricePerUnit { get; set; }
        public decimal? TaxRate { get; set; }

        public decimal Total => (PricePerUnit ?? 0) * (Quantity ?? 0); // Calculate the total price for the line item
    }

    public record ShippingInfo
    {
        public RecipientInfo? Recipient { get; set; }
        public AddressInfo? Address { get; set; }
        public string? Instructions { get; set; } = "";
        public string? Method { get; set; }
    }

    public record BillingInfo
    {
        public RecipientInfo? Recipient { get; set; }
        public AddressInfo? Address { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public record RecipientInfo
    {
        public string? Title { get; set; }
        public string? FullName { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public record AddressInfo
    {
        public string? Country { get; set; }
        public List<string>? Lines { get; set; }
    }
}