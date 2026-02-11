using System.Text;
using Asp.Versioning;
using FoodHub.Application;
using FoodHub.Application.Interfaces;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.BackgroundJobs;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Infrastructure.Services;
using FoodHub.Presentation.Middleware;
using FoodHub.WebAPI.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using StackExchange.Redis;

// Cấu hình Bootstrap Logger (để log ngay cả khi app chưa start được)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()) // Log JSON ra Console
    .CreateBootstrapLogger();

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ========================================
    // Load Environment Variables from .env file
    // ========================================
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
    }

    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    if (!string.IsNullOrEmpty(jwtSecret))
    {
        builder.Configuration["Jwt:SecretKey"] = jwtSecret;
    }

    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");

    if (!string.IsNullOrEmpty(jwtIssuer))
    {
        builder.Configuration["Jwt:Issuer"] = jwtIssuer;
    }

    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
    if (!string.IsNullOrEmpty(jwtAudience))
    {
        builder.Configuration["Jwt:Audience"] = jwtAudience;
    }

    var jwtAccessExpires = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRES_IN_MINUTE");
    if (!string.IsNullOrEmpty(jwtAccessExpires))
    {
        builder.Configuration["Jwt:ExpiresInMinute"] = jwtAccessExpires;
    }

    var jwtRefreshExpires = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS");
    if (!string.IsNullOrEmpty(jwtRefreshExpires))
    {
        builder.Configuration["Jwt:RefreshTokenExpiresInDays"] = jwtRefreshExpires;
    }

    var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbPassword))
    {
        var connectionString =
            $"Host={dbHost};Port={dbPort ?? "5432"};Database={dbName ?? "FoodHub"};Username={dbUser ?? "postgres"};Password={dbPassword}";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }

    var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Configuration["ConnectionStrings:Redis"] = redisConnection;
    }

    var emailSmtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
    var emailSmtpPort = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
    var emailSenderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL");
    var emailSenderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME");
    var emailAppPassword = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD");

    if (!string.IsNullOrEmpty(emailSmtpHost))
        builder.Configuration["EmailSettings:SmtpHost"] = emailSmtpHost;
    if (!string.IsNullOrEmpty(emailSmtpPort))
        builder.Configuration["EmailSettings:SmtpPort"] = emailSmtpPort;
    if (!string.IsNullOrEmpty(emailSenderEmail))
        builder.Configuration["EmailSettings:SenderEmail"] = emailSenderEmail;
    if (!string.IsNullOrEmpty(emailSenderName))
        builder.Configuration["EmailSettings:SenderName"] = emailSenderName;
    if (!string.IsNullOrEmpty(emailAppPassword))
        builder.Configuration["EmailSettings:AppPassword"] = emailAppPassword;

    var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
    if (!string.IsNullOrEmpty(allowedOrigins))
    {
        builder.Configuration["AllowedOrigins"] = allowedOrigins;
    }

    var cloudinaryCloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
    if (!string.IsNullOrEmpty(cloudinaryCloudName))
    {
        builder.Configuration["Cloudinary:CloudName"] = cloudinaryCloudName;
    }

    var cloudinaryApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
    if (!string.IsNullOrEmpty(cloudinaryApiKey))
    {
        builder.Configuration["Cloudinary:ApiKey"] = cloudinaryApiKey;
    }

    var cloudinaryApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
    if (!string.IsNullOrEmpty(cloudinaryApiSecret))
    {
        builder.Configuration["Cloudinary:ApiSecret"] = cloudinaryApiSecret;
    }

    var otlpEndpoint =
        Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
        ?? "http://localhost:4317";
    var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "FoodHub.WebAPI";

    // Cấu hình Serilog (Ghi đè Logger mặc định của .NET)
    // - ReadFrom.Services: Đọc cấu hình từ Dependency Injection
    // - Enrich: Bổ sung thông tin ngữ cảnh (MachineName, ThreadId...)
    // - WriteTo: Định nghĩa nơi lưu log (Console & File)
    builder.Host.UseSerilog(
        (context, services, configuration) =>
            configuration
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter()) // Xuất log ra Console dạng JSON
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    "Logs/foodhub-.log", // Tên file log: Logs/foodhub-20231010.log
                    rollingInterval: RollingInterval.Day
                ) // Tự động tạo file mới mỗi ngày
    );

    // Cấu hình OpenTelemetry (Giám sát hiệu năng & Truy vết)
    builder
        .Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName)) // Định nghĩa tên Service trong hệ thống giám sát
        .WithTracing(tracing =>
            tracing
                .AddAspNetCoreInstrumentation() // Theo dõi các Request vào Controller
                .AddHttpClientInstrumentation() // Theo dõi các Request ra ngoài bằng HttpClient
                .AddEntityFrameworkCoreInstrumentation() // Theo dõi các câu lệnh SQL (EF Core)
                .AddRedisInstrumentation(builder.Configuration.GetConnectionString("Redis")) // Theo dõi Redis
                .AddOtlpExporter(options => // Xuất data Tracing về Collector (Jaeger)
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                })
        )
        .WithMetrics(metrics =>
            metrics
                .AddAspNetCoreInstrumentation() // Thu thập metrics cơ bản (Request/sec, Duration...)
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation() // Thu thập metrics của .NET Runtime (GC, CPU, Memory...)
                .AddOtlpExporter(options => // Xuất metrics về Collector (Prometheus)
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                })
        );

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var connectionString =
            builder.Configuration.GetConnectionString("Redis")
            ?? builder.Configuration["Redis:ConnectionString"]
            ?? "localhost:6379";
        options.Configuration = connectionString;
        options.InstanceName = builder.Configuration["Redis:InstanceName"];
    });

    // Configure Forwarded Headers for Proxy/Load Balancer support
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    // Configure Lowercase URLs
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
    });

    // Add services to the container.
    builder.Services.AddControllers(opt =>
    {
        opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    // Configure API Versioning
    builder
        .Services.AddApiVersioning(options =>
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
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    // Configure Response Compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.MimeTypes =
            Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/json", "text/plain", "image/svg+xml" }
            );
    });

    builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(
        options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        }
    );

    builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(
        options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        }
    );

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen(c =>
    {
        c.CustomSchemaIds(type => type.FullName);

        // Config JWT in Swagger
        c.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
            }
        );

        c.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    new string[] { }
                },
            }
        );
    });

    //Setting for CORS - Load origins from configuration
    var corsOrigins =
        builder.Configuration["AllowedOrigins"]?.Split(',') ?? new[] { "http://localhost:3000" };

    builder.Services.AddCors(opt =>
    {
        opt.AddPolicy(
            "AllowReact",
            policy =>
                policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()
        ); // Allow Cookies
    });

    //Setting for JWT Authentication
    var jwt = builder.Configuration.GetSection("Jwt");
    builder
        .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Cookies["accessToken"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
            };
            opt.TokenValidationParameters =
                new Microsoft.IdentityModel.Tokens.TokenValidationParameters
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

    // Register Layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register Localization
    builder.Services.AddLocalization();

    var app = builder.Build();

    // Configure Localization Middleware
    var supportedCultures = new[] { "vi", "en" };
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture("vi")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    app.UseRequestLocalization(localizationOptions);

    app.UseForwardedHeaders();

    app.UseResponseCompression();

    app.UseMiddleware<ExceptionMiddleware>();

    // Kích hoạt Middleware ghi log request của Serilog
    // Nên đặt sau ExceptionMiddleware và trước các Middleware nghiệp vụ
    // Giúp ghi lại thông tin request (HTTP Method, Path, Status Code, Date...) gọn gàng
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        // Use Swashbuckle Swagger
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var descriptions = app.DescribeApiVersions();
            foreach (var description in descriptions)
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                var name = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint(url, name);
            }
        });

        // Auto Migrate & Seed Data
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var retryCount = 0;
            var maxRetries = 20; // Increased from 5

            while (retryCount < maxRetries)
            {
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var initializer = services.GetRequiredService<DbInitializer>();

                    context.Database.Migrate();
                    initializer.Initialize();
                    break; // Success, exit loop
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    if (retryCount >= maxRetries)
                    {
                        logger.LogError(
                            ex,
                            "An error occurred while migrating or seeding the database."
                        );
                        throw;
                    }

                    logger.LogWarning(
                        "Database not ready. Retry {Count}/{Max}...",
                        retryCount,
                        maxRetries
                    );
                    await Task.Delay(3000);
                }
            }
        }
    }

    app.UseCors("AllowReact");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
