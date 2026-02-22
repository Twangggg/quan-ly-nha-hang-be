using Asp.Versioning.ApiExplorer;
using FoodHub.Application;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Presentation.Middleware;
using FoodHub.WebAPI.Presentation.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Cấu hình Bootstrap Logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Bước 1: Cấu hình hệ thống cơ bản ---
    builder.AddEnvironmentVariables(); // Nạp biến môi trường từ file .env
    builder.AddCustomSerilog(); // Cấu hình ghi log (Console & File)

    // --- Bước 2: Đăng ký các dịch vụ từ các lớp (Layers) ---
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // --- Bước 3: Đăng ký các dịch vụ mở rộng (Presentation Extensions) ---
    builder.Services.AddMonitoring(builder.Configuration); // OpenTelemetry (Tracing & Metrics)
    builder.Services.AddSecurityServices(builder.Configuration); // JWT, CORS
    builder.Services.AddSwaggerDocumentation(); // Tài liệu API Swagger
    builder.Services.AddWebServices(builder.Configuration); // Redis, Rate Limiting, Versioning...
    builder.Services.AddHealthCheckServices(builder.Configuration); // Health Check (DB, Cache)

    var app = builder.Build();

    // --- Bước 4: Cấu hình Middleware Pipeline (Luồng xử lý Request) ---
    app.UseWebPresentation(); // Rate Limiting, Localization, Compression...
    app.UseMiddleware<ExceptionMiddleware>(); // Xử lý lỗi tập trung
    app.UseSerilogRequestLogging(); // Ghi log lỗi/Request tự động

    if (app.Environment.IsDevelopment())
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwaggerDocumentation(provider);

        // Auto Migrate & Seed Data
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var retryCount = 0;
            var maxRetries = 20;

            while (retryCount < maxRetries)
            {
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var initializer = services.GetRequiredService<DbInitializer>();

                    context.Database.Migrate();
                    initializer.Initialize();
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    if (retryCount >= maxRetries)
                    {
                        logger.LogError(
                            ex,
                            "An error occurred while migrating or seeding the database."
                        );
                        throw;
                    }

                    logger.LogWarning(
                        "Database not ready. Retry {Count}/{Max}...",
                        retryCount,
                        maxRetries
                    );
                    await Task.Delay(3000);
                }
            }
        }
    }

    app.UseCors("AllowReact");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication(); // Xác thực người dùng
    app.UseAuthorization(); // Phân quyền người dùng

    app.MapControllers(); // Map các API Controller
    app.MapHealthCheckEndpoints(); // GET /health & /health/detail

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
