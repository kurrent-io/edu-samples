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

    public async Task<List<ProductRanking>> GetTopProductsAsync(string hourKey)
    {
        var db = _redis.GetDatabase();
        var topProducts = await db.SortedSetRangeByScoreWithScoresAsync(hourKey, order: Order.Descending, take: 10);

        return topProducts.Select(p => new ProductRanking
        {
            ProductId = p.Element.ToString(),
            Quantity = (int)p.Score
        }).ToList();
    }

    public async Task<string?> GetFirstAvailableHourKeyAsync()
    {
        var db = _redis.GetDatabase();
        var keys = await db.ExecuteAsync("KEYS", $"{TopProductsKey}:*");
        return (((RedisResult[])keys)!).FirstOrDefault()?.ToString();
    }
}

public class ProductRanking
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
