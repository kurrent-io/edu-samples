using Npgsql;

namespace DemoWeb;

public class PostgresService
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresService> _logger;

    public PostgresService(IConfiguration configuration, ILogger<PostgresService> logger)
    {
        _logger = logger;
        var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        _connectionString = $"Host={postgresHost};Port=5432;Database=postgres;Username=postgres;Password=postgres";

        _logger.LogInformation("PostgreSQL connection string: {ConnectionString}",
            _connectionString.Replace("Password=", "Password=***"));
    }

    public async Task<List<CartItem>> GetCartItemsAsync(CartFilterOptions filterOptions)
    {
        try
        {
            _logger.LogInformation("Getting cart items with filter: {@FilterOptions}", filterOptions);
            var cartItems = new List<CartItem>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build the query with filter options and join
            var query = new System.Text.StringBuilder(@"
            SELECT c.cart_id, c.customer_id, c.status, 
                   ci.product_id, ci.product_name, ci.quantity, 
                   ci.price_per_unit, ci.updated_at
            FROM carts c
            JOIN cart_items ci ON c.cart_id = ci.cart_id
            WHERE 1=1");

            var parameters = new List<NpgsqlParameter>();

            // Add filters
            if (!string.IsNullOrEmpty(filterOptions.CartId))
            {
                query.Append(" AND c.cart_id LIKE @CartId");
                parameters.Add(new NpgsqlParameter("@CartId", $"%{filterOptions.CartId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.CustomerId))
            {
                query.Append(" AND c.customer_id LIKE @CustomerId");
                parameters.Add(new NpgsqlParameter("@CustomerId", $"%{filterOptions.CustomerId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.Status))
            {
                query.Append(" AND c.status = @Status");
                parameters.Add(new NpgsqlParameter("@Status", filterOptions.Status));
            }

            // Add sorting
            query.Append($" ORDER BY {filterOptions.SortColumn} {filterOptions.SortDirection}");

            // Add pagination
            query.Append(" LIMIT @PageSize OFFSET @Offset");
            parameters.Add(new NpgsqlParameter("@PageSize", filterOptions.PageSize));
            parameters.Add(new NpgsqlParameter("@Offset", (filterOptions.Page - 1) * filterOptions.PageSize));

            _logger.LogInformation("Executing SQL: {Sql}", query.ToString());

            using var cmd = new NpgsqlCommand(query.ToString(), connection);
            cmd.Parameters.AddRange(parameters.ToArray());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cartItems.Add(new CartItem
                {
                    CartId = reader.GetString(reader.GetOrdinal("cart_id")),
                    CustomerId = !reader.IsDBNull(reader.GetOrdinal("customer_id"))
                        ? reader.GetString(reader.GetOrdinal("customer_id"))
                        : null,
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    ProductId = reader.GetString(reader.GetOrdinal("product_id")),
                    ProductName = reader.GetString(reader.GetOrdinal("product_name")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                    PricePerUnit = reader.GetDecimal(reader.GetOrdinal("price_per_unit"))
                });
            }

            _logger.LogInformation("Retrieved {Count} cart items", cartItems.Count);
            return cartItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items");
            return new List<CartItem>(); // Return an empty list if there's an error
        }
    }

    public async Task<int> GetCartItemsTotalCountAsync(CartFilterOptions filterOptions)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build the count query with filter options and join
            var query = new System.Text.StringBuilder(@"
            SELECT COUNT(*) 
            FROM carts c
            JOIN cart_items ci ON c.cart_id = ci.cart_id
            WHERE 1=1");

            var parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrEmpty(filterOptions.CartId))
            {
                query.Append(" AND c.cart_id LIKE @CartId");
                parameters.Add(new NpgsqlParameter("@CartId", $"%{filterOptions.CartId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.CustomerId))
            {
                query.Append(" AND c.customer_id LIKE @CustomerId");
                parameters.Add(new NpgsqlParameter("@CustomerId", $"%{filterOptions.CustomerId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.Status))
            {
                query.Append(" AND c.status = @Status");
                parameters.Add(new NpgsqlParameter("@Status", filterOptions.Status));
            }

            _logger.LogInformation("Executing count SQL: {Sql}", query.ToString());

            using var cmd = new NpgsqlCommand(query.ToString(), connection);
            cmd.Parameters.AddRange(parameters.ToArray());

            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _logger.LogInformation("Total count: {Count}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items count");
            return 0;
        }
    }
}

public class CartItem
{
    public string CartId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CartFilterOptions
{
    public string? CartId { get; set; }
    public string? CustomerId { get; set; }
    public string? Status { get; set; } // Change to string type to handle empty string
    public string SortColumn { get; set; } = "created_at";
    public string SortDirection { get; set; } = "DESC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
