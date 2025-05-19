using Common;
namespace ReportProjection;

public class ReportProjection
{
    public static void ProjectOrderToReadModel(OrderPlaced orderPlaced, ReportReadModel readModel)
    {
        var orderDate = DateOnly.FromDateTime(orderPlaced.at!.Value.DateTime);
        var orderRegion = orderPlaced.store!.geographicRegion!;
            
        var finalDayOfTheMonth = new DateOnly(orderDate.Year, orderDate.Month,
            DateTime.DaysInMonth(orderDate.Year, orderDate.Month));

        ProjectToMonthEndReport();
        ProjectToMonthEndReportSnapshots();

        void ProjectToMonthEndReport()
        {
            ProjectToMonthlySales(finalDayOfTheMonth);

            if (orderDate == finalDayOfTheMonth)
            {
                ProjectToDailySales(finalDayOfTheMonth);
            }
        }
            
        void ProjectToMonthlySales(DateOnly reportDate)
        {
            var report = GetReportAsOf(reportDate);

            foreach (var lineItem in orderPlaced.lineItems!)
            {
                report.IncrementMonthlySales(lineItem.category, orderRegion, lineItem.total);
            }
        }

        void ProjectToDailySales(DateOnly reportDate)
        {
            var report = GetReportAsOf(reportDate);

            foreach (var lineItem in orderPlaced.lineItems!)
            {
                report.IncrementDailySales(lineItem.category, orderRegion, lineItem.total); 
            }
        }

        MonthlyReport GetReportAsOf(DateOnly asOfDate)
        {
            var report = readModel.GetReport(asOfDate);

            if (report != null) return report;
                
            report = new MonthlyReport(Config.GetReportedCategories(), Config.GetReportedRegions(),
                Config.GetSalesTarget().GetMonthlySalesTargetFor(asOfDate.Year, asOfDate.Month));

            readModel.AddOrUpdateReport(asOfDate, report);

            return report;
        }

        void ProjectToMonthEndReportSnapshots()
        {
            for (var i = orderDate.Day; i <= finalDayOfTheMonth.Day - 1; i++) // For each subsequent day in the month
            {
                var snapShotDate = new DateOnly(orderDate.Year, orderDate.Month, i);

                ProjectToMonthlySales(snapShotDate);
            }

            if (orderDate != finalDayOfTheMonth)
            {
                ProjectToDailySales(orderDate);
            }
        }
    }
}