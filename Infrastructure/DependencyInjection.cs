using FoodHub.Application.Interfaces;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Infrastructure.Persistence.Repositories;
using FoodHub.Infrastructure.Security;
using FoodHub.Infrastructure.Services;
using FoodHub.Infrastructure.Services.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;


namespace FoodHub.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    })
                .UseSnakeCaseNamingConvention();
            });

            // Register HttpContextAccessor
            services.AddHttpContextAccessor();

            // Register Redis Connection
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                return ConnectionMultiplexer.Connect(redisConnection);
            });

            // Register Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ITokenService, JwtTokenService>();

            // Rate Limiting Service
            services.AddScoped<IRateLimiter, RedisRateLimiter>();

            // Email Service
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
