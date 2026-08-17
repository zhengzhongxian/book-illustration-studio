using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Studio.Api.Application.Services;
using Studio.Api.Infrastructure.Concurrency;
using Studio.Api.Infrastructure.Data;
using Studio.Api.Infrastructure.Gemini;
using Studio.Api.Infrastructure.Middleware;
using Studio.Api.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Database & SQLite Directory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=data/studio.db";

// Ensure data folder exists
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<StudioDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// 2. Options Configuration
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

// Allow environment variable override for API Key
var envApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (!string.IsNullOrWhiteSpace(envApiKey))
{
    builder.Services.PostConfigure<GeminiOptions>(opt => opt.ApiKey = envApiKey);
}

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
