using System.Text.Json;
using Common;

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

var salesDataFilepath = Environment.GetEnvironmentVariable("SALES_DATA_FILEPATH") ?? "data/report.json";

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/api/sales-data", () =>
{
    if (!File.Exists(salesDataFilepath))
        throw new FileNotFoundException("Sales read model data not found", salesDataFilepath);
    
    var salesDataJson = File.ReadAllText(salesDataFilepath);                // Read the sales report JSON file
    var salesData = JsonSerializer.Deserialize<ReportReadModel>(salesDataJson);   // Deserialize the JSON into a ReportReadModel object
    
    return salesData;
});

app.UseStaticFiles();

app.MapFallbackToFile("index.html"); // Serve wwwroot/index.html which is built by Vite

app.Run($"http://0.0.0.0:{port}");
