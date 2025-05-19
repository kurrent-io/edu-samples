using Common;
namespace ReportProjection;

public class ReportProjection
{
    // ---------------------------------------------------------------------------------------------------------
    // Projects an OrderPlaced event into the ReportReadModel by updating monthly and daily sales numbers.
    //
    // This includes:
    // - Incrementing monthly sales totals for each line item in the order, for the order's region and category.
    // - Updating the month-end report for the last day of the order's month.
    // - Creating daily and monthly sales snapshots for each day from the order date up to the end of the month.
    // - Ensuring the read model contains up-to-date reports for all relevant dates, categories, and regions.
    // ---------------------------------------------------------------------------------------------------------
    public static void ProjectOrderToReadModel(OrderPlaced orderPlaced, 
        ReportReadModel readModel)
    {
        var orderDate = DateOnly.FromDateTime(orderPlaced.at!.Value.DateTime);  // Convert the order date to DateOnly without time 

        var orderRegion = orderPlaced.store!.geographicRegion!;                 // Get the order region from the store object

        var finalDayOfTheMonth =                                                // Get the last day of the month for the order date
            new DateOnly(orderDate.Year, orderDate.Month, 
                DateTime.DaysInMonth(orderDate.Year, orderDate.Month));

        ProjectToMonthEndReport();                                              // Project the order to the month-end report
        ProjectToMonthEndReportSnapshots();                                     // Project the order to the month-end report snapshots

        void ProjectToMonthEndReport()                                          // Project the order to the monthly sales for the last day of the month
        {
            ProjectToMonthlySales(finalDayOfTheMonth);                          // Project to monthly sales for the last day of the month

            if (orderDate == finalDayOfTheMonth)                                // If the order date is the last day of the month{
                ProjectToDailySales(finalDayOfTheMonth);                        // Project the order's total to daily sales for the last day of the month

        }

        // Project the order's totals to monthly sales
        void ProjectToMonthlySales(DateOnly reportDate)
        {
            var report = GetReportAsOf(reportDate);                             // Get the report as of the requested date
            foreach (var lineItem in orderPlaced.lineItems!)                    // Iterate through each line item in the order
            {
                report.IncrementMonthlySales(lineItem.category, orderRegion,    // Increment the monthly sales by line item's total
                    lineItem.total);                                             
            }
        }

        // Project the order's totals to daily sales
        void ProjectToDailySales(DateOnly reportDate)
        {
            var report = GetReportAsOf(reportDate);                             // Get the report as of the requested date
            foreach (var lineItem in orderPlaced.lineItems!)                    // Iterate through each line item in the order
            {
                report.IncrementDailySales(lineItem.category, orderRegion,      // Increment the daily sales by line item's total
                    lineItem.total); 
            }
        }

        // Get the report as of the requested date
        MonthEndReport GetReportAsOf(DateOnly asOfDate)
        {
            var report = readModel.GetReport(asOfDate);                         // Get the report as of the requested date

            if (report != null) return report;                                  // If the report already exists, return it

            report = new MonthEndReport(                                        // Otherwise, create a new report
                Config.GetCategoriesToReport(),                                 // Based on the configured categories 
                Config.GetRegionsToReport(),                                    // and regions to report on
                Config.GetSalesTarget().                                        // and the sales target for the month
                    GetMonthEndSalesTargetFor(asOfDate.Year, asOfDate.Month));  // of the requested date

            readModel.AddOrUpdateReport(asOfDate, report);                      // Add or update the report in the read model

            return report;
        }

        void ProjectToMonthEndReportSnapshots()
        {
            for (var i = orderDate.Day; i <= finalDayOfTheMonth.Day - 1; i++)   // For each subsequent day in the month after the order date
            {
                var snapShotDate = new DateOnly(orderDate.Year, 
                    orderDate.Month, i);

                ProjectToMonthlySales(snapShotDate);                            // Project the monthly sales to a snapshot of the report
            }

            if (orderDate != finalDayOfTheMonth)                                // Don't project daily sales if the order date
            {                                                                   // is the final day of the month
                ProjectToDailySales(orderDate);                                 // since it is already projected
            }                                                                   // by ProjectToMonthEndReport
        }
    }
}