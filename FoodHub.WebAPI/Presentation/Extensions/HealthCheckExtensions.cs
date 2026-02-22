using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Đăng ký Health Check cho PostgreSQL và Redis
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var pgConnectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Thiếu connection string 'DefaultConnection' cho PostgreSQL."
            );

        var redisConnectionString =
            configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"]
            ?? "localhost:6379";

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString: pgConnectionString,
                name: "postgresql",
                tags: ["db", "ready"]
            )
            .AddRedis(
                redisConnectionString: redisConnectionString,
                name: "redis",
                tags: ["cache", "ready"]
            );

        return services;
    }

    /// <summary>
    /// Map 2 endpoint health check:
    /// - GET /health        → trả về Healthy/Unhealthy đơn giản (dùng cho Docker/K8s probe)
    /// - GET /health/detail → trả về JSON chi tiết từng dependency
    /// </summary>
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        // Endpoint đơn giản
        endpoints.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });

        // Endpoint chi tiết (JSON đầy đủ)
        endpoints.MapHealthChecks(
            "/health/detail",
            new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            }
        );

        return endpoints;
    }
}
