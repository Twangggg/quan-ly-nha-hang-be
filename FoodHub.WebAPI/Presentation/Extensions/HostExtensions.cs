using Serilog;
using Serilog.Formatting.Compact;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class HostExtensions
{
    /// <summary>
    /// Nạp các biến môi trường từ file .env lên hệ thống Configuration của .NET
    /// </summary>
    public static void AddEnvironmentVariables(this WebApplicationBuilder builder)
    {
        // 1. Tìm file .env bằng cách quét ngược từ thư mục hiện tại lên cha
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".env")))
        {
            dir = dir.Parent;
        }

        if (dir != null)
        {
            DotNetEnv.Env.Load(Path.Combine(dir.FullName, ".env"));
        }

        // 2. Map các biến môi trường vào Configuration (để có thể inject IConfiguration)
        var config = builder.Configuration;

        void MapEnv(string envVar, string configKey)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value))
                config[configKey] = value;
        }

        MapEnv("JWT_SECRET_KEY", "Jwt:SecretKey");
        MapEnv("JWT_ISSUER", "Jwt:Issuer");
        MapEnv("JWT_AUDIENCE", "Jwt:Audience");
        MapEnv("JWT_ACCESS_TOKEN_EXPIRES_IN_MINUTE", "Jwt:ExpiresInMinute");
        MapEnv("JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS", "Jwt:RefreshTokenExpiresInDays");

        // 3. Xây dựng Chuỗi kết nối Database (PostgreSQL) từ .env
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "FoodHub";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbPassword))
        {
            config["ConnectionStrings:DefaultConnection"] =
                $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
        }

        MapEnv("REDIS_CONNECTION", "ConnectionStrings:Redis");
        MapEnv("EMAIL_SMTP_HOST", "EmailSettings:SmtpHost");
        MapEnv("EMAIL_SMTP_PORT", "EmailSettings:SmtpPort");
        MapEnv("EMAIL_SENDER_EMAIL", "EmailSettings:SenderEmail");
        MapEnv("EMAIL_SENDER_NAME", "EmailSettings:SenderName");
        MapEnv("EMAIL_APP_PASSWORD", "EmailSettings:AppPassword");
        MapEnv("ALLOWED_ORIGINS", "AllowedOrigins");
        MapEnv("CLOUDINARY_CLOUD_NAME", "Cloudinary:CloudName");
        MapEnv("CLOUDINARY_API_KEY", "Cloudinary:ApiKey");
        MapEnv("CLOUDINARY_API_SECRET", "Cloudinary:ApiSecret");
    }

    /// <summary>
    /// Cấu hình ghi log sử dụng Serilog thay thế cho logger mặc định của .NET
    /// </summary>
    public static void AddCustomSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            (context, services, configuration) =>
                configuration
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .WriteTo.Console(new CompactJsonFormatter())
                    .WriteTo.File(
                        new CompactJsonFormatter(),
                        "Logs/foodhub-.log",
                        rollingInterval: RollingInterval.Day
                    )
        );
    }
}
