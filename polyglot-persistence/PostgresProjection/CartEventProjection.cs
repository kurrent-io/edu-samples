namespace PostgresProjection;

using SqlStatement = (string Text, object Params);

public static class CartEventProjection
{
    public static SqlStatement ProjectToSqlStatement(this Event evt)
    {
        return evt switch
        {
            VisitorStartedShopping e => Project(e),
            CustomerStartedShopping e => Project(e),
            CartShopperGotIdentified e => Project(e),
            CartGotCheckedOut e => Project(e),
            CartGotAbandoned e => Project(e),
            _ => throw new Exception($"Unknown event type: {evt.GetType().Name}")
        };
    }

    public static SqlStatement Project(VisitorStartedShopping evt)
    {
        return (@"INSERT INTO carts(cart_id, customer_id, status, created_at, updated_at)
                VALUES(@CartId, null, @Status, @Timestamp, @Timestamp)
                ON CONFLICT(cart_id) DO NOTHING",
            (CartId: evt.cartId, Status: "STARTED", Timestamp: evt.when));
    }

    public static SqlStatement Project(CustomerStartedShopping evt)
    {
        return (@"INSERT INTO carts(cart_id, customer_id, status, created_at, updated_at)
                VALUES(@CartId, @CustomerId, @Status, @Timestamp, @Timestamp)
                ON CONFLICT(cart_id) DO NOTHING",
            (CartId: evt.cartId, CustomerId: evt.customerId, Status: "STARTED", Timestamp: evt.when));
    }

    public static SqlStatement Project(CartShopperGotIdentified evt)
    {
        return (@"UPDATE carts
                SET customer_id = @CustomerId,
                    updated_at = @Timestamp
                WHERE cart_id = @CartId",
            (CartId: evt.cartId, CustomerId: evt.customerId, Timestamp: evt.when));
    }

    public static SqlStatement Project(CartGotCheckedOut evt)
    {
        return (@"UPDATE carts
                SET status = @Status,
                    updated_at = @Timestamp
                WHERE cart_id = @CartId",
            (CartId: evt.cartId, Status: "CHECKED_OUT", Timestamp: evt.when));
    }

    public static SqlStatement Project(CartGotAbandoned evt)
    {
        return (@"UPDATE carts
                SET status = @Status,
                    updated_at = @Timestamp
                WHERE cart_id = @CartId",
            (CartId: evt.cartId, Status: "ABANDONED", Timestamp: evt.when));
    }
}