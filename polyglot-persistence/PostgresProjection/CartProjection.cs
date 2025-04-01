using Dapper;
using EventStore.Client;

namespace PostgresProjection;

public static class CartProjection
{
    private const string ReadModelName = "carts";

    public static CommandDefinition? ProjectToSqlCommand(ResolvedEvent evt)
    {
        var decodeEvent = ResolvedEventEncoder.DecodeEvent(evt);
        
        switch (decodeEvent)
        {
            case VisitorStartedShopping visitor:
                return Project(visitor);
            case CustomerStartedShopping customer:
                return Project(customer);
            case CartShopperGotIdentified identified:
                return Project(identified);
            case CartGotCheckedOut checkedOut:
                return Project(checkedOut);
            case CartGotAbandoned abandoned:
                return Project(abandoned);
            default:
                return null;
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
    public static CommandDefinition GetCartCheckpointQuery()
    {
        var sql = "SELECT checkpoint FROM checkpoints WHERE read_model_name = @ReadModelName";

        var parameters = new { ReadModelName };

        return new CommandDefinition(sql, parameters);
    }

    public static CommandDefinition? GetCheckpointUpdateCommand(long checkpoint)
    {
        var sql = @"
                    INSERT INTO checkpoints (read_model_name, checkpoint)
                    VALUES (@ReadModelName, @Checkpoint)
                    ON CONFLICT (read_model_name) DO UPDATE 
                    SET checkpoint = @Checkpoint";

        var parameters = new { ReadModelName, Checkpoint = checkpoint };

        return new CommandDefinition(sql, parameters);
    }
}
