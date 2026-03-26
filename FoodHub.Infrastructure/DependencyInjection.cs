using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Infrastructure.BackgroundJobs;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Infrastructure.Persistence.Repositories;
using FoodHub.Infrastructure.Security;
using FoodHub.Infrastructure.Services.Inventory;
using FoodHub.Infrastructure.Services.Reservations;
using FoodHub.Infrastructure.Services.Reporting;
using FoodHub.Infrastructure.Services.External;
using FoodHub.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using FoodHub.Infrastructure.Services.Common.RateLimiting;
using FoodHub.Infrastructure.Services.Common;
using FoodHub.Infrastructure.Services.Messaging;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Features.Reservations.Services;

namespace FoodHub.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
            services.Configure<PayOsSettings>(configuration.GetSection("PayOS"));

            // Configure QuestPDF License
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            services.AddDbContext<AppDbContext>(options =>
            {
                options
                    .UseNpgsql(
                        configuration.GetConnectionString("DefaultConnection"),
                        npgsqlOptions =>
                        {
                            npgsqlOptions.MigrationsAssembly(
                                typeof(AppDbContext).Assembly.FullName
                            );
                        }
                    )
                    .UseSnakeCaseNamingConvention();
            });
            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAuditLogService, Services.AuditLogService>();

            // Register Redis Connection
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString =
                    configuration.GetConnectionString("Redis") ?? "localhost:6379";
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
            services.AddSingleton<KdsPriorityCalculator>();
            services.AddScoped<DbInitializer>();

            // Rate Limiting Service
            services.AddScoped<IRateLimiter, RedisRateLimiter>();

            // Cache Service
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<
                IInventoryAvailabilitySyncService,
                InventoryAvailabilitySyncService
            >();

            // Cloudinary Service
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IPaymentService, PayOsService>();

            // SignalR Service
            services.AddScoped<ISignalRService, SignalRService>();

            // Excel Export Service
            services.AddScoped<ISalesExcelService, SalesExcelService>();
            services.AddScoped<IAttendanceExcelService, AttendanceExcelService>();

            // PDF Export Service
            services.AddScoped<IPdfService, PdfService>();

            // Inventory Services
            services.AddScoped<IInventoryDeductionService, InventoryDeductionService>();
            services.AddScoped<IReceiptCodeGenerator, ReceiptCodeGenerator>();

            // Reservation Services
            services.AddScoped<IReservationSettingsProvider, ReservationSettingsProvider>();
            services.AddScoped<IReservationLifecyclePolicy, ReservationLifecyclePolicy>();

            // Authorization Services
            services.AddSingleton<IPermissionProvider, PermissionProvider>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            // Background Email Services
            services.AddSingleton<BackgroundEmailChannel>();
            services.AddSingleton<IBackgroundEmailSender>(sp =>
                sp.GetRequiredService<BackgroundEmailChannel>()
            );
            services.AddHostedService<EmailBackgroundWorker>();
            services.AddHostedService<ReservationLifecycleWorker>();

            return services;
        }
    }
}
