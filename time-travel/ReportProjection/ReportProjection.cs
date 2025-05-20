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
    public static void ProjectOrderToReadModel(OrderPlaced orderPlaced, ReportReadModel readModel)
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
            var report = GetReportAsOf(readModel, reportDate);                  // Get the report as of the requested date
            foreach (var lineItem in orderPlaced.lineItems!)                    // Iterate through each line item in the order
            {
                report.IncrementMonthlySales(lineItem.category, orderRegion,    // Increment the monthly sales by line item's total
                    lineItem.total);                                             
            }
        }

        // Project the order's totals to daily sales
        void ProjectToDailySales(DateOnly reportDate)
        {
            var report = GetReportAsOf(readModel, reportDate);                  // Get the report as of the requested date
            foreach (var lineItem in orderPlaced.lineItems!)                    // Iterate through each line item in the order
            {
                report.IncrementDailySales(lineItem.category, orderRegion,      // Increment the daily sales by line item's total
                    lineItem.total); 
            }
        }

        void ProjectToMonthEndReportSnapshots()
        {
            /*
            TODO:
            Implement the projection so that read model includes
            sales data for every day of the month, not just the most
            recent day

            The JSON read model will look something like this:

            {
                "checkpoint": 42,
                "salesReports": {
                "2025-01-31": {
                    "categorySalesReports": {
                        "Electronics": {
                            "regionSalesReports": {
             	                "Asia": {
             		                "dailySales": 5200.25,
             		                "targetSales": 6000.00,
             		                "totalMonthlySales": 60200.90,
             		                "targetHitRate": 86.71
             	                }
                            }
                        }
                    }
                },    
                "2025-01-30": {
                    "categorySalesReports": {
                        "Electronics": {
                            "regionSalesReports": {
             	                "Asia": {
             		                "dailySales": 5000.50,
             		                "targetSales": 6000.00,
             		                "totalMonthlySales": 55000.75,
             		                "targetHitRate": 83.34
             	                }
                            }
                        }
                    }
                }
                // Capture state of the read model for rest of the month
                // "2025-01-29": {}
                // "2025-01-28": {}
                }
            }

            Testing Instructions
            ------------
            1. Run `./scripts/stop-app.sh` to stop the projection app
            2. Run `./scripts/delete-read-model.sh` to delete the read model
            3. Make updates to this method
            4. Run `./scripts/start-app.sh` to start the projection app
            5. Run `docker compose --profile app logs -f` to check if it ran properly
            6. Run `code ./data/report-read-model.json` to check the read model.
               The figures should match the ones in `./data/report-read-model-expected.json`
            8. Repeat 1. to 7. until the results match

            Tips
            ----
            You should be able to achieve this by using the variables
            and methods above such as:
            - orderDate
            - finalDayOfTheMonth
            - ProjectToMonthlySales()
            - ProjectToDailySales()

            And you should be able to do so without modifying any
            other method or classes in this project.

            Solution
            --------
            You can find the solution to this problem in the
            file `SolutionToProjectToMonthEndReportSnapshots.txt`
            */
        }
    }

    // Get the report as of the requested date
    private static MonthEndReport GetReportAsOf(ReportReadModel readModel, DateOnly asOfDate)
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

}