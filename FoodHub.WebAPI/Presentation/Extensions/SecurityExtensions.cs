using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class SecurityExtensions
{
    /// <summary>
    /// Cấu hình các dịch vụ liên quan đến Bảo mật (CORS, Authentication)
    /// </summary>
    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // 1. Cấu hình CORS (Cho phép Frontend gọi API từ domain khác)
        var corsOrigins =
            configuration["AllowedOrigins"]?.Split(',') ?? new[] { "http://localhost:3000" };
        services.AddCors(opt =>
        {
            opt.AddPolicy(
                "AllowReact",
                policy =>
                    policy
                        .WithOrigins(corsOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
            );
        });

        // 2. Cấu hình xác thực (Authentication) sử dụng JWT Token
        var jwt = configuration.GetSection("Jwt");
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Cho phép lấy Token từ Cookie (trường hợp Frontend lưu token trong cookie)
                        var accessToken = context.Request.Cookies["accessToken"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                };
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt["SecretKey"]!)
                    ),
                };
            });

        // 3. Cấu hình chống tấn công CSRF (Cross-Site Request Forgery)
        // Sử dụng cơ chế Double Submit Cookie cho Single Page Application (React)
        services.AddAntiforgery(options =>
        {
            // Tên Header mà Frontend (React) sẽ gửi kèm mã token
            options.HeaderName = "X-XSRF-TOKEN";

            // Cấu hình Cookie chứa mã token
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // Phải để false để Javascript của React đọc được mã
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        return services;
    }
}
