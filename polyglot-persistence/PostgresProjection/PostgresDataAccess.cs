using Dapper;
using Npgsql;

namespace PostgresProjection
{
    public class PostgresDataAccess
    {
        private readonly NpgsqlConnection _connection;
        private NpgsqlTransaction? _transaction;

        public PostgresDataAccess(NpgsqlConnection connection)
        {
            _connection = connection;
            _connection.Open();

            CreateTablesIfNotExist();
        }

        private void CreateTablesIfNotExist()
        {
            _connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS carts (
                        cart_id TEXT PRIMARY KEY,
                        customer_id TEXT NULL,
                        status TEXT NOT NULL DEFAULT 'STARTED',
                        created_at TIMESTAMP NOT NULL,
                        updated_at TIMESTAMP NOT NULL
                    )");

            _connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS checkpoints (
                        read_model_name TEXT PRIMARY KEY,
                        checkpoint BIGINT NOT NULL
                    )");
        }

        public void InsertCart(string cartId, string customerId, string status, DateTimeOffset timestamp)
        {
            _connection.Execute(@"
                    INSERT INTO carts (cart_id, customer_id, status, created_at, updated_at)
                    VALUES (@CartId, @CustomerId, @Status, @Timestamp, @Timestamp)
                    ON CONFLICT (cart_id) DO NOTHING",
                new { CartId = cartId, CustomerId = customerId, Status = status, Timestamp = timestamp.UtcDateTime },
                _transaction);
        }

        public void UpdateCartCustomer(string cartId, string customerId, DateTimeOffset timestamp)
        {
            _connection.Execute(@"
                    UPDATE carts 
                    SET customer_id = @CustomerId,
                        updated_at = @Timestamp
                    WHERE cart_id = @CartId",
                new { CartId = cartId, CustomerId = customerId, Timestamp = timestamp.UtcDateTime },
                _transaction);
        }

        public void UpdateCartStatus(string cartId, string status, DateTimeOffset timestamp)
        {
            _connection.Execute(@"
                    UPDATE carts 
                    SET status = @Status,
                        updated_at = @Timestamp
                    WHERE cart_id = @CartId",
                new { CartId = cartId, Status = status, Timestamp = timestamp.UtcDateTime },
                _transaction);
        }

        public long? GetCheckpoint(string readModelName)
        {
            return _connection.QueryFirstOrDefault<long?>(
                "SELECT checkpoint FROM checkpoints WHERE read_model_name = @ReadModelName",
                new { ReadModelName = readModelName });
        }

        public void UpdateCheckpoint(string readModelName, long checkpoint)
        {
            _connection.Execute(@"
                    INSERT INTO checkpoints (read_model_name, checkpoint)
                    VALUES (@ReadModelName, @Checkpoint)
                    ON CONFLICT (read_model_name) DO UPDATE 
                    SET checkpoint = @Checkpoint",
                new { ReadModelName = readModelName, Checkpoint = checkpoint },
                _transaction);
        }

        // Transaction management methods

        public void BeginTransaction()
        {
            _transaction = _connection.BeginTransaction();
        }

        public void Commit()
        {   
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
        }
    }
}