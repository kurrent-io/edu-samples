using Common;
using Microsoft.Extensions.Configuration;

namespace ReportProjection
{
    public class Config
    {
        private static readonly IConfigurationRoot _configuration;

        static Config()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static List<string> GetCategories()
        {
            return _configuration.GetSection("Categories").Get<List<string>>() ?? new List<string>();
        }

        public static List<string> GetRegions()
        {
            return _configuration.GetSection("Regions").Get<List<string>>() ?? new List<string>();
        }

        public static TargetMonthlySales GetTargetMonthlySales(int year, int month)
        {
            var result = new Dictionary<string, Dictionary<string, int>>();
            string period = $"{year:D4}-{month:D2}";
            var categoriesSection = _configuration.GetSection($"TargetMonthlySales:{period}:Categories");
            if (!categoriesSection.Exists())
                return new TargetMonthlySales(result);

            foreach (var category in categoriesSection.GetChildren())
            {
                var regions = category.GetSection("Regions").Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();
                result[category.Key] = regions;
            }
            return new TargetMonthlySales(result);
        }

    }
}