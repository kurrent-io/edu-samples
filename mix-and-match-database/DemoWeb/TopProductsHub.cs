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

    public async Task GetTopProducts()
    {
        var topProducts = await _redisService.GetTopProductsAsync();
        await Clients.Caller.SendAsync("ReceiveTopProducts", topProducts);
    }
}