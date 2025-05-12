using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReportProjection
{
    public record SalesReport
    {
        [JsonPropertyName("reportDate")]
        public string ReportDate { get; init; }

        [JsonPropertyName("salesData")]
        public List<CategorySalesData> SalesData { get; init; }
    }

    public record CategorySalesData
    {
        [JsonPropertyName("category")]
        public string Category { get; init; }

        [JsonPropertyName("Asia")]
        public RegionSalesData Asia { get; init; }

        [JsonPropertyName("Europe")]
        public RegionSalesData Europe { get; init; }

        [JsonPropertyName("North America")]
        public RegionSalesData NorthAmerica { get; init; }

        [JsonPropertyName("Middle East")]
        public RegionSalesData MiddleEast { get; init; }
    }

    public record RegionSalesData
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

    // Root list type for deserialization
    public class ReportReadModel : List<SalesReport> { }
}