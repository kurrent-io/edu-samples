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
        private Dictionary<DateOnly, MonthEndReport> MonthEndReportsBySnapshotDate { get; init; } = new();
        
        public MonthEndReport? GetReport(DateOnly snapshotDate)
        {
            return MonthEndReportsBySnapshotDate.TryGetValue(snapshotDate, out var salesReport) ? salesReport : null;
        }

        public void AddOrUpdateReport(DateOnly snapshotDate, MonthEndReport report)
        {
            if (MonthEndReportsBySnapshotDate.ContainsKey(snapshotDate))
                MonthEndReportsBySnapshotDate[snapshotDate] = report;
            else
                MonthEndReportsBySnapshotDate.Add(snapshotDate, report);
        }
    }

    public class MonthEndReport
    {
        [JsonPropertyName("categories")]
        public Dictionary<string, Category> Categories { get; set; } = new();

        public MonthEndReport() { } // Parameter-less constructor for deserialization

        public MonthEndReport(List<string> reportedCategories, List<string> reportedRegions, MonthEndSalesTarget? monthEndSalesTarget)
        {
            foreach (var categoryToReport in reportedCategories)
            {
                var category = new Category();

                foreach (var regionToReport in reportedRegions)
                {
                    category.Regions[regionToReport] = new Region
                    {
                        MonthEndSalesTarget = monthEndSalesTarget?.GetSalesTargetBy(categoryToReport, regionToReport) ?? 0
                    };
                }

                Categories[categoryToReport] = category;
            }
        }

        public void IncrementDailySales(string category, string region, decimal amount)
        {
            if (!Categories.TryGetValue(category, out var salesByCategory) ||
                !salesByCategory.Regions.TryGetValue(region, out var salesByCategoryAndRegion)) return;

            salesByCategoryAndRegion.DailySales += amount;
        }

        public void IncrementMonthlySales(string? category, string region, decimal amount)
        {
            if (string.IsNullOrEmpty(category)) return;

            if (!Categories.TryGetValue(category, out var salesByCategory) ||
                !salesByCategory.Regions.TryGetValue(region, out var salesByCategoryAndRegion)) return;

            salesByCategoryAndRegion.TotalMonthlySales += amount;
            salesByCategoryAndRegion.TargetHitRate = salesByCategoryAndRegion.MonthEndSalesTarget == 0
                ? 0
                : Math.Round(salesByCategoryAndRegion.TotalMonthlySales / salesByCategoryAndRegion.MonthEndSalesTarget, 2);
        }

    }

    public record Category
    {
        [JsonPropertyName("regions")]
        public Dictionary<string, Region> Regions { get; set; } = new();
    }
    
    public record Region
    {
        [JsonPropertyName("dailySales")]
        public decimal DailySales { get; set; }

        [JsonPropertyName("monthEndSalesTarget")]
        public decimal MonthEndSalesTarget { get; set; }

        [JsonPropertyName("totalMonthlySales")]
        public decimal TotalMonthlySales { get; set; }

        [JsonPropertyName("targetHitRate")]
        public decimal TargetHitRate { get; set; }
    }

    public class MonthEndSalesTargets
    {
        [JsonPropertyName("MonthEndSalesTarget")]
        private readonly Dictionary<(int year, int month), MonthEndSalesTarget> _sales;

        public MonthEndSalesTargets(Dictionary<(int year, int month), MonthEndSalesTarget> sales)
        {
            _sales = sales;
        }

        public MonthEndSalesTarget? GetMonthEndSalesTargetFor(int year, int month)
        {
            return _sales.TryGetValue(new (year, month), out var monthlySales) ? monthlySales : null;
        }
    }

    public class MonthEndSalesTarget
    {
        private readonly Dictionary<string, Dictionary<string, int>> _salesTargets;

        public MonthEndSalesTarget(Dictionary<string, Dictionary<string, int>> salesTargets)
        {
            _salesTargets = salesTargets;
        }
        
        public decimal? GetSalesTargetBy(string category, string region)
        {
            if (_salesTargets.TryGetValue(category, out var regions) &&
                regions.TryGetValue(region, out var target))
            {
                return target;
            }
            return null;
        }
    }
}