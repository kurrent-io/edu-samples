using Npgsql;

namespace DemoWeb.Services;

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

    public async Task<List<Cart>> GetCartsAsync(CartFilterOptions filterOptions)
    {
        try
        {
            _logger.LogInformation("Getting carts with filter: {@FilterOptions}", filterOptions);
            var carts = new List<Cart>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build the query with filter options
            var query = new System.Text.StringBuilder("SELECT * FROM carts WHERE 1=1");
            var parameters = new List<NpgsqlParameter>();

            // Add filters
            if (!string.IsNullOrEmpty(filterOptions.CartId))
            {
                query.Append(" AND cart_id LIKE @CartId");
                parameters.Add(new NpgsqlParameter("@CartId", $"%{filterOptions.CartId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.CustomerId))
            {
                query.Append(" AND customer_id LIKE @CustomerId");
                parameters.Add(new NpgsqlParameter("@CustomerId", $"%{filterOptions.CustomerId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.Status))
            {
                query.Append(" AND status = @Status");
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
                carts.Add(new Cart
                {
                    CartId = reader.GetString(reader.GetOrdinal("cart_id")),
                    CustomerId = !reader.IsDBNull(reader.GetOrdinal("customer_id"))
                        ? reader.GetString(reader.GetOrdinal("customer_id"))
                        : null,
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            _logger.LogInformation("Retrieved {Count} carts", carts.Count);
            return carts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carts");
            return new List<Cart>(); // Return an empty list if the table does not exist
        }
    }

    public async Task<int> GetCartsTotalCountAsync(CartFilterOptions filterOptions)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build the count query with filter options
            var query = new System.Text.StringBuilder("SELECT COUNT(*) FROM carts WHERE 1=1");
            var parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrEmpty(filterOptions.CartId))
            {
                query.Append(" AND cart_id LIKE @CartId");
                parameters.Add(new NpgsqlParameter("@CartId", $"%{filterOptions.CartId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.CustomerId))
            {
                query.Append(" AND customer_id LIKE @CustomerId");
                parameters.Add(new NpgsqlParameter("@CustomerId", $"%{filterOptions.CustomerId}%"));
            }

            if (!string.IsNullOrEmpty(filterOptions.Status))
            {
                query.Append(" AND status = @Status");
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
            _logger.LogError(ex, "Error getting carts count");
            return 0;
        }
    }
}


public class Cart
{
    public string CartId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
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
