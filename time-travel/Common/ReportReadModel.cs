using System.Text.Json.Serialization;

namespace Common
{
    // Root type for deserialization
    public class ReportReadModel
    {
        [JsonPropertyName("checkpoint")]
        public long Checkpoint { get; set; }

        [JsonPropertyName("salesReports")]
        [JsonInclude]
        private Dictionary<DateOnly, MonthlyReport> SalesReports { get; init; } = new();
        
        public MonthlyReport? GetReport(DateOnly asOfDate)
        {
            return SalesReports.TryGetValue(asOfDate, out var salesReport) ? salesReport : null;
        }

        public void AddOrUpdateReport(DateOnly asOfDate, MonthlyReport report)
        {
            if (SalesReports.ContainsKey(asOfDate))
                SalesReports[asOfDate] = report;
            else
                SalesReports.Add(asOfDate, report);
        }
    }

    public class MonthlyReport
    {
        [JsonPropertyName("categorySalesReports")]
        public Dictionary<string, CategorySalesReport> CategorySalesReports { get; set; } = new();

        public MonthlyReport(List<string> reportedCategories, List<string> reportedRegions, TargetMonthlySales? targetMonthlySales)
        {
            foreach (var category in reportedCategories)
            {
                var categorySalesReport = new CategorySalesReport();

                foreach (var region in reportedRegions)
                {
                    categorySalesReport.RegionSalesReports[region] = new RegionSalesReport
                    {
                        TargetSales = targetMonthlySales?.GetTargetSales(category, region) ?? 0
                    };
                }

                CategorySalesReports[category] = categorySalesReport;
            }
        }

        public void IncrementDailySales(string category, string region, decimal amount)
        {
            if (!CategorySalesReports.TryGetValue(category, out var categorySalesReport) ||
                !categorySalesReport.RegionSalesReports.TryGetValue(region, out var regionSalesReport)) return;

            regionSalesReport.DailySales += amount;
        }

        public void IncrementMonthlySales(string? category, string region, decimal amount)
        {
            if (string.IsNullOrEmpty(category)) return;

            if (!CategorySalesReports.TryGetValue(category, out var categorySalesReport) ||
                !categorySalesReport.RegionSalesReports.TryGetValue(region, out var regionSalesReport)) return;

            regionSalesReport.TotalMonthlySales += amount;
            regionSalesReport.TargetHitRate = regionSalesReport.TargetSales == 0
                ? 0
                : Math.Round(regionSalesReport.TotalMonthlySales / regionSalesReport.TargetSales, 2);
        }

    }

    public record CategorySalesReport
    {
        [JsonPropertyName("regionSalesReports")]
        public Dictionary<string, RegionSalesReport> RegionSalesReports { get; set; } = new();
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

    public class TargetSales
    {
        [JsonPropertyName("TargetMonthlySales")]
        private readonly Dictionary<(int year, int month), TargetMonthlySales> _sales;

        public TargetSales(Dictionary<(int year, int month), TargetMonthlySales> sales)
        {
            _sales = sales ?? new();
        }

        /// <summary>
        /// Gets the TargetMonthlySales for a specific year and month, or null if not found.
        /// </summary>
        public TargetMonthlySales? GetMonthlySalesTargetFor(int year, int month)
        {
            return _sales.TryGetValue(new (year, month), out var monthlySales) ? monthlySales : null;
        }
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