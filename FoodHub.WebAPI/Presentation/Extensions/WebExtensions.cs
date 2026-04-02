using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class WebExtensions
{
    private static readonly HashSet<string> CsrfExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/refresh-token",
        "/api/v1/auth/csrf-token",
        "/api/v1/auth/logout",
        "/api/v1/auth/request-password-reset",
        "/api/v1/auth/reset-password",
        "/hubs/kds/negotiate",
        "/hubs/billing/negotiate",
        "/hubs/table-status/negotiate",
    };

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
                    opt.PermitLimit = 1000;
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
                        PermitLimit = 1000,
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
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // 4. Cấu hình URL (Tự động viết thường URL: /api/Values -> /api/values)
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        // Controllers
        services
            .AddControllers(opt =>
            {
                opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
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

        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

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

        // --- Cấu hình Anti-CSRF Token cho Frontend ---
        app.UseAntiforgery();
        app.Use(
            async (context, next) =>
            {
                if (
                    HttpMethods.IsPost(context.Request.Method)
                    || HttpMethods.IsPut(context.Request.Method)
                    || HttpMethods.IsPatch(context.Request.Method)
                    || HttpMethods.IsDelete(context.Request.Method)
                )
                {
                    var path = context.Request.Path.Value ?? string.Empty;
                    if (CsrfExemptPaths.Contains(path))
                    {
                        await next(context);
                        return;
                    }

                    var hasAuthCookie =
                        context.Request.Cookies.ContainsKey("accessToken")
                        || context.Request.Cookies.ContainsKey("refreshToken");

                    if (hasAuthCookie)
                    {
                        try
                        {
                            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                            await antiforgery.ValidateRequestAsync(context);
                        }
                        catch (AntiforgeryValidationException ex)
                        {
                            var logger = context.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("FoodHub.Antiforgery");
                            var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
                            var csrfHeader = context.Request.Headers["X-XSRF-TOKEN"].ToString();
                            var csrfCookie = context.Request.Cookies["XSRF-TOKEN"];

                            logger.LogWarning(
                                ex,
                                "CSRF validation failed for {Method} {Path}. HasAccessToken={HasAccessToken}, HasRefreshToken={HasRefreshToken}, HasCsrfHeader={HasCsrfHeader}, HasCsrfCookie={HasCsrfCookie}, Error={Error}",
                                context.Request.Method,
                                path,
                                context.Request.Cookies.ContainsKey("accessToken"),
                                context.Request.Cookies.ContainsKey("refreshToken"),
                                !string.IsNullOrWhiteSpace(csrfHeader),
                                !string.IsNullOrWhiteSpace(csrfCookie),
                                ex.Message
                            );

                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            context.Response.ContentType = "application/json";
                            var payload = env.IsDevelopment()
                                ? $$"""
                                {"statusCode":400,"message":"Invalid or missing CSRF token.","detail":"{{System.Text.Json.JsonEncodedText.Encode(ex.Message)}}"}
                                """
                                : """
                                {"statusCode":400,"message":"Invalid or missing CSRF token."}
                                """;
                            await context.Response.WriteAsync(payload);
                            return;
                        }
                    }
                }

                await next(context);
            }
        );

        app.UseRateLimiter(); // Giới hạn tần suất gọi API

        return app;
    }
}
