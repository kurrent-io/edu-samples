namespace PostgresProjection
{
    public abstract record Event;

    public record VisitorStartedShopping : Event
    {
        public string cartId { get; set; }
        public DateTimeOffset when { get; set; }
    }

    public record CustomerStartedShopping : Event
    {
        public string cartId { get; set; }
        public string customerId { get; set; }
        public DateTimeOffset when { get; set; }
    }

    public record CartShopperGotIdentified : Event
    {
        public string cartId { get; set; }
        public string customerId { get; set; }
        public DateTimeOffset when { get; set; }
    }

    public record ItemGotAdded : Event
    {
        public string cartId { get; set; }
        public string productId { get; set; }
        public string productName { get; set; }
        public int quantity { get; set; }
        public decimal pricePerUnit { get; set; }
        public decimal taxRate { get; set; }
        public DateTimeOffset when { get; set; }
    }

    public record ItemGotRemoved : Event
    {
        public string cartId { get; set; }
        public string productId { get; set; }
        public int quantity { get; set; }
        public DateTimeOffset when { get; set; }
    }

    public record CartGotCheckedOut : Event
    {
        public string cartId { get; set; }
        public string orderId { get; set; }
        public DateTimeOffset when { get; set; }
    }

    public record CartGotAbandoned : Event
    {
        public string cartId { get; set; }
        public string afterBeingIdleFor { get; set; }
        public DateTimeOffset when { get; set; }
    }
}
