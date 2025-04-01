using DemoWeb.Services;
using Microsoft.AspNetCore.SignalR;

namespace DemoWeb.Hubs;

public class TopProductsHub : Hub
{
    private readonly RedisService _redisService;

    public TopProductsHub(RedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task GetTopProducts(string hourKey)
    {
        string key = hourKey;
        if (string.IsNullOrEmpty(hourKey))
        {
            // look for the first "top products" key in redis
            key = await _redisService.GetFirstAvailableHourKeyAsync();

            // return nothing if not found
            if (key == null) return;
        }

        var topProducts = await _redisService.GetTopProductsAsync(key);
        await Clients.Caller.SendAsync("ReceiveTopProducts", topProducts, key.Split(':').Last());
    }
}