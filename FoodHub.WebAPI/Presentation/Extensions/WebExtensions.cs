using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class WebExtensions
{
    /// <summary>
    /// Cấu hình các dịch vụ Web cơ bản (Redis, Rate Limit, Versioning...)
    /// </summary>
    public static IServiceCollection AddWebServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // 1. Cấu hình Redis Cache (Dùng để lưu Session/Cache dữ liệu)
        services.AddStackExchangeRedisCache(options =>
        {
            var connectionString =
                configuration.GetConnectionString("Redis")
                ?? configuration["Redis:ConnectionString"]
                ?? "localhost:6379";
            options.Configuration = connectionString;
            options.InstanceName = configuration["Redis:InstanceName"];
        });

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(
                policyName: "fixed",
                opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                }
            );
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString()
                        ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                    }
                )
            );
        });

        // 3. Cấu hình Forwarded Headers (Hỗ trợ khi chạy sau Proxy/Load Balancer)
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        // 4. Cấu hình URL (Tự động viết thường URL: /api/Values -> /api/values)
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        // Controllers
        services.AddControllers(opt =>
        {
            opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        // API Versioning
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("x-api-version")
                );
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Định dạng group (ví dụ: v1)
                options.SubstituteApiVersionInUrl = true;
            });

        // 6. Cấu hình Nén phản hồi (Response Compression - Giúp tải nhanh hơn)
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
            options.MimeTypes =
                Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/json", "text/plain", "image/svg+xml" }
                );
        });

        services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(
            options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Fastest;
            }
        );

        services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(
            options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Fastest;
            }
        );

        // Localization
        services.AddLocalization();

        return services;
    }

    /// <summary>
    /// Kích hoạt các Middleware đã cấu hình ở trên vào Pipeline
    /// </summary>
    public static IApplicationBuilder UseWebPresentation(this IApplicationBuilder app)
    {
        // Cấu hình Đa ngôn ngữ (Localization)
        var supportedCultures = new[] { "vi", "en" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture("vi")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        app.UseRequestLocalization(localizationOptions);

        app.UseForwardedHeaders(); // Xử lý proxy header
        app.UseResponseCompression(); // Nén dữ liệu trả về
        app.UseRateLimiter(); // Giới hạn tần suất gọi API

        return app;
    }
}
