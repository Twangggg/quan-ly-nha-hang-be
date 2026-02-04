using FoodHub.Application.Interfaces;
using FoodHub.Infrastructure.BackgroundJobs;
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
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

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
            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Register Redis Connection
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false; // Allow app to start even if Redis is down
                return ConnectionMultiplexer.Connect(options);
            });

            // Register Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<DbInitializer>();

            // Rate Limiting Service
            services.AddScoped<IRateLimiter, RedisRateLimiter>();

            // Cache Service
            services.AddScoped<ICacheService, RedisCacheService>();


            // Register Background Email Services
            services.AddSingleton<BackgroundEmailChannel>();
            services.AddSingleton<IBackgroundEmailSender>(provider => provider.GetRequiredService<BackgroundEmailChannel>());
            services.AddHostedService<EmailBackgroundWorker>();

            return services;
        }
    }
}
