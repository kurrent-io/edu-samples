using Common;
using Dapper;
using EventStore.Client;

namespace PostgresProjection;

public static class CartProjection
{
    public const string ReadModelName = "carts";

    public static IEnumerable<CommandDefinition> Project(ResolvedEvent evt)
    {
        var decodedEvent = CartEventEncoder.Decode(evt.Event.Data, evt.Event.EventType);

        CommandDefinition? command = decodedEvent switch
        {
            VisitorStartedShopping visitor => Project(visitor),
            CustomerStartedShopping customer => Project(customer),
            CartShopperGotIdentified identified => Project(identified),
            CartGotCheckedOut checkedOut => Project(checkedOut),
            CartGotAbandoned abandoned => Project(abandoned),
            _ => null
        };

        if (command != null)
        {
            yield return command.Value;
        }
    }

    private static CommandDefinition Project(VisitorStartedShopping evt)
    {
        var sql = @"INSERT INTO carts(cart_id, customer_id, status, created_at, updated_at)
                VALUES(@CartId, null, @Status, @Timestamp, @Timestamp)
                ON CONFLICT(cart_id) DO NOTHING";

        var parameters = new { CartId = evt.cartId, Status = "STARTED", Timestamp = evt.at };

        return new CommandDefinition(sql, parameters);
    }

    private static CommandDefinition Project(CustomerStartedShopping evt)
    {
        var sql = @"INSERT INTO carts(cart_id, customer_id, status, created_at, updated_at)
                VALUES(@CartId, @CustomerId, @Status, @Timestamp, @Timestamp)
                ON CONFLICT(cart_id) DO NOTHING";

        var parameters = new { CartId = evt.cartId, CustomerId = evt.customerId, Status = "STARTED", Timestamp = evt.at };

        return new CommandDefinition(sql, parameters);
    }

    private static CommandDefinition Project(CartShopperGotIdentified evt)
    {
        var sql = @"UPDATE carts
                SET customer_id = @CustomerId,
                    updated_at = @Timestamp
                WHERE cart_id = @CartId";

        var parameters = new { CartId = evt.cartId, CustomerId = evt.customerId, Timestamp = evt.at };

        return new CommandDefinition(sql, parameters);
    }

    private static CommandDefinition Project(CartGotCheckedOut evt)
    {
        var sql = @"UPDATE carts
                SET status = @Status,
                    updated_at = @Timestamp
                WHERE cart_id = @CartId";

        var parameters = new { CartId = evt.cartId, Status = "CHECKED_OUT", Timestamp = evt.at };

        return new CommandDefinition(sql, parameters);
    }

    private static CommandDefinition Project(CartGotAbandoned evt)
    {
        var sql = @"UPDATE carts
                SET status = @Status,
                    updated_at = @Timestamp
                WHERE cart_id = @CartId";

        var parameters = new { CartId = evt.cartId, Status = "ABANDONED", Timestamp = evt.at };

        return new CommandDefinition(sql, parameters);
    }

    public static CommandDefinition GetCreateCartTableCommand()
    {
        return new CommandDefinition(@"
                     CREATE TABLE IF NOT EXISTS carts (
                         cart_id TEXT PRIMARY KEY,
                         customer_id TEXT NULL,
                         status TEXT NOT NULL DEFAULT 'STARTED',
                         created_at TIMESTAMP NOT NULL,
                         updated_at TIMESTAMP NOT NULL
                     )");
    }
}