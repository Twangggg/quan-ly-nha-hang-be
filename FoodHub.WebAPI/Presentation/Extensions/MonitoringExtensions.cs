using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class MonitoringExtensions
{
    /// <summary>
    /// Cấu hình OpenTelemetry để thu thập dữ liệu về Tracing và Metrics
    /// Điều này giúp theo dõi hiệu năng và lỗi của hệ thống (Observability)
    /// </summary>
    public static IServiceCollection AddMonitoring(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "FoodHub.WebAPI";

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                // Thu thập log từ các tiến trình xử lý Request, Web Client và Database
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                var redisConn = configuration.GetConnectionString("Redis");
                if (!string.IsNullOrEmpty(redisConn))
                {
                    tracing.AddRedisInstrumentation(redisConn);
                }

                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            })
            .WithMetrics(metrics =>
                // Thu thập các chỉ số về RAM, CPU, Request count...
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    })
            );

        return services;
    }
}
