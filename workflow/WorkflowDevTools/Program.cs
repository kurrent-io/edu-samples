using EventStore.Client;


Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");

// -------------------- //
// Connect to KurrentDB //
// -------------------- //

var kurrentDbHost = Environment.GetEnvironmentVariable("KURRENTDB_HOST")    // Get the KurrentDB host from environment variable
                    ?? "localhost";                                         // Default to localhost if not set

var kurrentdb = new EventStoreClient(                                       // Create a connection to KurrentDB
                EventStoreClientSettings.Create(
                  $"esdb://{kurrentDbHost}:2113?tls=false"));

// -------------------- //
// Setup the web server //
// -------------------- //

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    then => then.UseSpa(spa =>
    {
        const int port = 5173;
 
        spa.Options.SourcePath = "client";
        spa.Options.DevServerPort = port;
        spa.Options.PackageManagerCommand = "npm";
 
        if (app.Environment.IsDevelopment())
        {
            spa.UseProxyToSpaDevelopmentServer($"http://localhost:{port}");
        }
    }));


// -------------------- //
// Start the web server //
// -------------------- //

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

app.Run($"http://0.0.0.0:{port}");