namespace PostgresProjection
{
    public record VisitorStartedShopping
    {
        public string cartId { get; set; }
        public DateTimeOffset at { get; set; }
    }

    public record CustomerStartedShopping
    {
        public string cartId { get; set; }
        public string customerId { get; set; }
        public DateTimeOffset at { get; set; }
    }

    public record CartShopperGotIdentified
    {
        public string cartId { get; set; }
        public string customerId { get; set; }
        public DateTimeOffset at { get; set; }
    }

    public record ItemGotAdded
    {
        public string cartId { get; set; }
        public string productId { get; set; }
        public string productName { get; set; }
        public int quantity { get; set; }
        public string currency { get; set; }
        public decimal pricePerUnit { get; set; }
        public decimal taxRate { get; set; }
        public DateTimeOffset at { get; set; }
    }

    public record ItemGotRemoved
    {
        public string cartId { get; set; }
        public string productId { get; set; }
        public int quantity { get; set; }
        public DateTimeOffset at { get; set; }
    }

    public record CartGotCheckedOut
    {
        public string cartId { get; set; }
        public string orderId { get; set; }
        public DateTimeOffset at { get; set; }
    }

    public record CartGotAbandoned
    {
        public string cartId { get; set; }
        public string afterBeingIdleFor { get; set; }
        public DateTimeOffset at { get; set; }
    }
}