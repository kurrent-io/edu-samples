using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Region = string;
using Category = string;
using ReportDate = string;

namespace ReportProjection
{
    // Root type for deserialization
    public record ReportReadModel
    {
        [JsonPropertyName("checkpoint")]
        public long Checkpoint { get; set; }

        [JsonPropertyName("salesReports")]
        public Dictionary<ReportDate, SalesReport> SalesReports { get; init; } = new();
    }
    
    public record SalesReport
    {
        [JsonPropertyName("categorySalesReports")]
        public Dictionary<Category, CategorySalesReport> CategorySalesReports { get; init; } = new();
    }
    
    public record CategorySalesReport
    {
        [JsonPropertyName("regionSalesReports")]
        public Dictionary<Region, RegionSalesReport> RegionSalesReports { get; init; } = new();
   }
    
    public record RegionSalesReport
    {
        [JsonPropertyName("dailySales")]
        public decimal DailySales { get; init; }

        [JsonPropertyName("targetSales")]
        public decimal TargetSales { get; init; }

        [JsonPropertyName("totalMonthlySales")]
        public decimal TotalMonthlySales { get; init; }

        [JsonPropertyName("targetHitRate")]
        public decimal TargetHitRate { get; init; }
    }
}