using Npgsql;

namespace PersistentSubscriptionOrderProcessor
{
    public class OrderFulfillmentRepository
    {
        private readonly PostgresDataAccess _dataAccess;

        public OrderFulfillmentRepository(PostgresDataAccess dataAccess)
        {
            _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS OrderFulfillment (
                    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    OrderId VARCHAR(255) UNIQUE NOT NULL,
                    Status VARCHAR(50) NOT NULL,
                    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                );";

            _dataAccess.Execute(sql);
        }

        public void StartOrderFulfillment(string? orderId)
        {
            if (string.IsNullOrEmpty(orderId))
                throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

            var sql = @"
                INSERT INTO OrderFulfillment (OrderId, Status)
                VALUES (@OrderId, 'Started')";
            
            try
            {
                _dataAccess.Execute(sql, new { OrderId = orderId });
                Console.WriteLine($"Order fulfillment for {orderId} started.");

            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
            {
                Console.WriteLine($"Order fulfillment for {orderId} already started. Ignoring start request.");
            }
        }
    }
}