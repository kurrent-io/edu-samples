namespace Common;
public record VisitorStartedShopping
{
    public required string cartId { get; set; }
    public DateTimeOffset at { get; set; }
}

public record CustomerStartedShopping
{
    public required string cartId { get; set; }
    public required string customerId { get; set; }
    public DateTimeOffset at { get; set; }
}

public record CartShopperGotIdentified
{
    public required string cartId { get; set; }
    public required string customerId { get; set; }
    public DateTimeOffset at { get; set; }
}

public record ItemGotAdded
{
    public string? cartId { get; set; }
    public string? productId { get; set; }
    public string? productName { get; set; }
    public int quantity { get; set; }
    public string? currency { get; set; }
    public decimal pricePerUnit { get; set; }
    public decimal taxRate { get; set; }
    public DateTimeOffset at { get; set; }
}

public record ItemGotRemoved
{
    public required string cartId { get; set; }
    public required string productId { get; set; }
    public int quantity { get; set; }
    public DateTimeOffset at { get; set; }
}

public record CartGotCheckedOut
{
    public required string cartId { get; set; }
    public required string orderId { get; set; }
    public DateTimeOffset at { get; set; }
}

public record CartGotAbandoned
{
    public required string cartId { get; set; }
    public required string afterBeingIdleFor { get; set; }
    public DateTimeOffset at { get; set; }
}