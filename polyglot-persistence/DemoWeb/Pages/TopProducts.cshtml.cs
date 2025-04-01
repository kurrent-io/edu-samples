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

        public string CurrentHour { get; private set; }

        public async Task OnGetAsync()
        {
            //var firstAvailableHour = await _redisService.GetFirstAvailableHourKeyAsync();
            //if (firstAvailableHour != null)
            //{

            //    CurrentHour = firstAvailableHour?.Split(':').Last();
            //}
        }
    }
}