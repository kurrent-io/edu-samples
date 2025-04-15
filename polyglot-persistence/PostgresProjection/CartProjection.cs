using Common;
using Dapper;
using EventStore.Client;

namespace PostgresProjection;

public static class CartProjection
{
    public static IEnumerable<CommandDefinition>? Project(ResolvedEvent evt)
    {
        var decodedEvent = CartEventEncoder.Decode(evt.Event.Data, evt.Event.EventType);

        IEnumerable<CommandDefinition>? command = decodedEvent switch
        {
            VisitorStartedShopping visitor => Project(visitor),
            CustomerStartedShopping customer => Project(customer),
            CartShopperGotIdentified identified => Project(identified),
            CartGotCheckedOut checkedOut => Project(checkedOut),
            CartGotAbandoned abandoned => Project(abandoned),
            ItemGotAdded added => Project(added),
            ItemGotRemoved removed => Project(removed),
            _ => null
        };

        return command;
    }

    private static IEnumerable<CommandDefinition>? Project(VisitorStartedShopping evt)
    {
        var sql = @"INSERT INTO carts(cart_id, customer_id, status, created_at, updated_at)
                        VALUES(@CartId, null, @Status, @Timestamp, @Timestamp)
                        ON CONFLICT(cart_id) DO NOTHING";

        var parameters = new { CartId = evt.cartId, Status = "STARTED", Timestamp = evt.at };

        yield return new CommandDefinition(sql, parameters);
    }

    private static IEnumerable<CommandDefinition>? Project(CustomerStartedShopping evt)
    {
        var sql = @"INSERT INTO carts(cart_id, customer_id, status, created_at, updated_at)
                        VALUES(@CartId, @CustomerId, @Status, @Timestamp, @Timestamp)
                        ON CONFLICT(cart_id) DO NOTHING";

        var parameters = new { CartId = evt.cartId, CustomerId = evt.customerId, Status = "STARTED", Timestamp = evt.at };

        yield return new CommandDefinition(sql, parameters);
    }

    private static IEnumerable<CommandDefinition>? Project(CartShopperGotIdentified evt)
    {
        var sql = @"UPDATE carts
                        SET customer_id = @CustomerId,
                            updated_at = @Timestamp
                        WHERE cart_id = @CartId";

        var parameters = new { CartId = evt.cartId, CustomerId = evt.customerId, Timestamp = evt.at };

        yield return new CommandDefinition(sql, parameters);
    }

    private static IEnumerable<CommandDefinition>? Project(CartGotCheckedOut evt)
    {
        var sql = @"UPDATE carts
                        SET status = @Status,
                            updated_at = @Timestamp
                        WHERE cart_id = @CartId";

        var parameters = new { CartId = evt.cartId, Status = "CHECKED_OUT", Timestamp = evt.at };

        yield return new CommandDefinition(sql, parameters);
    }

    private static IEnumerable<CommandDefinition>? Project(CartGotAbandoned evt)
    {
        var sql = @"UPDATE carts
                        SET status = @Status,
                            updated_at = @Timestamp
                        WHERE cart_id = @CartId";

        var parameters = new { CartId = evt.cartId, Status = "ABANDONED", Timestamp = evt.at };

        yield return new CommandDefinition(sql, parameters);
    }

    private static IEnumerable<CommandDefinition>? Project(ItemGotAdded evt)
    {
        var sql = @"INSERT INTO cart_items(
                    cart_id, 
                    product_id, 
                    product_name, 
                    quantity, 
                    currency, 
                    price_per_unit, 
                    tax_rate, 
                    updated_at)
                VALUES(
                    @CartId, 
                    @ProductId, 
                    @ProductName, 
                    @Quantity, 
                    @Currency, 
                    @PricePerUnit, 
                    @TaxRate, 
                    @Timestamp)
                ON CONFLICT(cart_id, product_id) 
                DO UPDATE SET 
                    quantity = cart_items.quantity + @Quantity,
                    updated_at = @Timestamp";

        var parameters = new
        {
            CartId = evt.cartId,
            ProductId = evt.productId,
            ProductName = evt.productName,
            Quantity = evt.quantity,
            Currency = evt.currency,
            PricePerUnit = evt.pricePerUnit,
            TaxRate = evt.taxRate,
            Timestamp = evt.at
        };

        yield return new CommandDefinition(sql, parameters);
    }

    private static IEnumerable<CommandDefinition>? Project(ItemGotRemoved evt)
    {
        var updateSql = @"UPDATE cart_items
                    SET quantity = quantity - @Quantity,
                        updated_at = @Timestamp
                    WHERE cart_id = @CartId AND product_id = @ProductId;";

        var parameters = new
        {
            CartId = evt.cartId,
            ProductId = evt.productId,
            Quantity = evt.quantity,
            Timestamp = evt.at
        };

        yield return new CommandDefinition(updateSql, parameters);

        var deleteSql = @"DELETE FROM cart_items
                    WHERE cart_id = @CartId AND product_id = @ProductId AND quantity = 0;";

        yield return new CommandDefinition(deleteSql, parameters);

    }


    public static IEnumerable<CommandDefinition> GetCreateTableCommand()
    {
        yield return new CommandDefinition(@"
                     CREATE TABLE IF NOT EXISTS carts (
                         cart_id TEXT PRIMARY KEY,
                         customer_id TEXT NULL,
                         status TEXT NOT NULL DEFAULT 'STARTED',
                         created_at TIMESTAMP NOT NULL,
                         updated_at TIMESTAMP NOT NULL
                     )");

        yield return new CommandDefinition(@"
                     CREATE TABLE IF NOT EXISTS cart_items (
                         cart_id TEXT NOT NULL,
                         product_id TEXT NOT NULL,
                         product_name TEXT NOT NULL,
                         quantity INTEGER NOT NULL,
                         currency TEXT NULL,
                         price_per_unit DECIMAL(10,2) NOT NULL,
                         tax_rate DECIMAL(5,2) NOT NULL,
                         updated_at TIMESTAMP NOT NULL,
                         PRIMARY KEY (cart_id, product_id),
                         FOREIGN KEY (cart_id) REFERENCES carts(cart_id) ON DELETE CASCADE
                     )");
    }

}
