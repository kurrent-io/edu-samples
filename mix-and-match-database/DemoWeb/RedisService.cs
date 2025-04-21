using StackExchange.Redis;

namespace DemoWeb.Services;

public class RedisService
{
    public const string TopProductsKey = "top-10-products";
    private readonly IConnectionMultiplexer _redis;


    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<List<ProductRanking>> GetTopProductsAsync()
    {
        var db = _redis.GetDatabase();
        var combinedResults = new Dictionary<string, double>();

        // Get the current date/time
        var currentDateTime = DateTime.UtcNow;

        // Process the last 24 hours
        for (int i = 0; i < 24; i++)
        {
            var hourToProcess = currentDateTime.AddHours(-i);
            var hourKeyToUse = $"{TopProductsKey}:{hourToProcess:yyyyMMddHH}";

            // Get all products for this hour
            var productsForHour = await db.SortedSetRangeByScoreWithScoresAsync(hourKeyToUse, order: Order.Descending);

            // Add to combined results
            foreach (var product in productsForHour)
            {
                var productId = product.Element.ToString();
                if (combinedResults.ContainsKey(productId))
                {
                    combinedResults[productId] += product.Score;
                }
                else
                {
                    combinedResults[productId] = product.Score;
                }
            }
        }

        // Sort by quantity and take top 10
        var topProducts = combinedResults
            .OrderByDescending(p => p.Value)
            .Take(10)
            .Select(p => new ProductRanking
            {
                ProductId = db.HashGet("product-names", p.Key),
                Quantity = (int)p.Value
            })
            .ToList();

        return topProducts;
    }
}

public class ProductRanking
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
