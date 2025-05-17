using System.Text.Json.Serialization;
using Region = string;
using Category = string;
using ReportDate = string;
using System.Globalization;

namespace Common
{
    // Root type for deserialization
    public record ReportReadModel
    {
        [JsonPropertyName("checkpoint")]
        public long Checkpoint { get; set; }

        [JsonPropertyName("salesReports")]
        public Dictionary<ReportDate, SalesReport> SalesReports { get; set; } = new();
    }
    
    public record SalesReport
    {
        [JsonPropertyName("categorySalesReports")]
        public Dictionary<Category, CategorySalesReport> CategorySalesReports { get; set; } = new();
    }

    public class Helper
    {
        public static SalesReport Create(int year, int month, IEnumerable<string> categories, IEnumerable<string> regions,
            TargetMonthlySales target)
        {
            var salesReport = new SalesReport();
            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var isoDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                salesReport.CategorySalesReports = new Dictionary<Category, CategorySalesReport>();

                foreach (var category in categories)
                {
                    var categorySalesReport = new CategorySalesReport
                    {
                        RegionSalesReports = new Dictionary<Region, RegionSalesReport>()
                    };

                    foreach (var region in regions)
                    {
                        categorySalesReport.RegionSalesReports[region] = new RegionSalesReport
                        {
                            DailySales = 0,
                            TargetSales = target.GetTargetSales(category, region) ?? 0,
                            TotalMonthlySales = 0,
                            TargetHitRate = 0
                        };
                    }

                    salesReport.CategorySalesReports[category] = categorySalesReport;
                }
            }

            return salesReport;
        }
    }

    public record CategorySalesReport
    {
        [JsonPropertyName("regionSalesReports")]
        public Dictionary<Region, RegionSalesReport> RegionSalesReports { get; set; } = new();
    }
    
    public record RegionSalesReport
    {
        [JsonPropertyName("dailySales")]
        public decimal DailySales { get; set; }

        [JsonPropertyName("targetSales")]
        public decimal TargetSales { get; set; }

        [JsonPropertyName("totalMonthlySales")]
        public decimal TotalMonthlySales { get; set; }

        [JsonPropertyName("targetHitRate")]
        public decimal TargetHitRate { get; set; }
    }

    public class TargetMonthlySales
    {
        private readonly Dictionary<string, Dictionary<string, int>> _targetSales;

        public TargetMonthlySales(Dictionary<string, Dictionary<string, int>> targetSales)
        {
            _targetSales = targetSales ?? new Dictionary<string, Dictionary<string, int>>();
        }

        /// <summary>
        /// Gets the target sales for a given category and region.
        /// Returns null if the category or region does not exist.
        /// </summary>
        public decimal? GetTargetSales(string category, string region)
        {
            if (_targetSales.TryGetValue(category, out var regions) &&
                regions.TryGetValue(region, out var target))
            {
                return target;
            }
            return null;
        }
    }

}