using Common;
using Microsoft.Extensions.Configuration;

namespace ReportProjection
{
    public class Config
    {
        private static readonly IConfigurationRoot Configuration;

        static Config()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static List<string> GetCategoriesToReport()
        {
            return Configuration.GetSection("CategoriesToReport").Get<List<string>>() ?? new List<string>();
        }

        public static List<string> GetRegionsToReport()
        {
            return Configuration.GetSection("RegionsToReport").Get<List<string>>() ?? new List<string>();
        }

        public static MonthEndSalesTargets GetSalesTarget()
        {
            var result = new Dictionary<(int year, int month), MonthEndSalesTarget>();
            var section = Configuration.GetSection("MonthEndSalesTarget");
            if (!section.Exists())
                return new MonthEndSalesTargets(result);

            foreach (var periodSection in section.GetChildren())
            {
                var period = periodSection.Key; // e.g., "2025-01"
                if (DateTime.TryParseExact(period, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var dt))
                {
                    var categoriesSection = periodSection.GetSection("Categories");
                    var targetSales = new Dictionary<string, Dictionary<string, int>>();

                    foreach (var categorySection in categoriesSection.GetChildren())
                    {
                        var regions = categorySection.GetSection("Regions").Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();
                        targetSales[categorySection.Key] = regions;
                    }

                    result[(dt.Year, dt.Month)] = new MonthEndSalesTarget(targetSales);
                }
            }

            return new MonthEndSalesTargets(result);
        }
    }
}