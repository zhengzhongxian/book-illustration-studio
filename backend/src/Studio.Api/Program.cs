using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Studio.Api.Application.Services;
using Studio.Api.Infrastructure.Concurrency;
using Studio.Api.Infrastructure.Data;
using Studio.Api.Infrastructure.Gemini;
using Studio.Api.Infrastructure.Middleware;
using Studio.Api.Infrastructure.Storage;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Literate,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// 0. Load .env into Environment Variables before building Configuration
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// 1. Database & SQLite Directory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=data/studio.db";

// Ensure data folder exists
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<StudioDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
});

// 2. Options Configuration: Pure Native .NET Options Pattern
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

// 3. Infrastructure & Services
builder.Services.AddHttpClient<IGeminiClient, GeminiRestClient>((sp, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddSingleton<IProjectLockService, ProjectLockService>();
builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPipelineService, PipelineService>();

// 4. Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// 5. CORS for Vite Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6. Ensure Database Schema & Enable WAL Mode
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StudioDbContext>();
    db.Database.EnsureCreated();

    // Enable WAL mode on SQLite for high-concurrency read/write
    try
    {
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to enable SQLite WAL pragmas.");
    }
}

// 7. Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();

app.Run();
