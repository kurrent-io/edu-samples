using DemoWeb.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoWeb.Pages
{
    public class TopProductsModel : PageModel
    {
        private readonly RedisService _redisService;

        public TopProductsModel(RedisService redisService)
        {
            _redisService = redisService;
        }
    }
}