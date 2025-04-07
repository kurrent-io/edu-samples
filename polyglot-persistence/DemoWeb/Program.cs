using StackExchange.Redis;
using DemoWeb.Hubs;
using DemoWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Register SignalR
builder.Services.AddSignalR();

// Register Redis services
var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisHost}:6379"));
builder.Services.AddSingleton<RedisService>();

// Register PostgreSQL service
builder.Services.AddScoped<PostgresService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Redirect root URL to TopProducts page
app.MapGet("/", context => {
    context.Response.Redirect("/TopProducts");
    return Task.CompletedTask;
});

// Map endpoints
app.MapRazorPages();

// Map SignalR hub
app.MapHub<TopProductsHub>("/topProductsHub");

app.Run();

Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started");