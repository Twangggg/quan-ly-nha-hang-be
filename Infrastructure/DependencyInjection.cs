using FoodHub.Application.Interfaces;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Infrastructure.Persistence.Repositories;
using FoodHub.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;


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
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<IPasswordHasher, PasswordHasher>();

            services.AddScoped<DbInitializer>();

            // Security Services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<DbInitializer>();

            return services;
        }
    }
}
