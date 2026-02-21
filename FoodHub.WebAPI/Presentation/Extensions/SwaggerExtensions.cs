using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace FoodHub.WebAPI.Presentation.Extensions;

public static class SwaggerExtensions
{
    /// <summary>
    /// Cấu hình Swagger để tạo tài liệu API tự động
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Hiển thị tên class đầy đủ để tránh trùng lặp schema
            c.CustomSchemaIds(type => type.FullName);

            // Cấu hình để Swagger có thể gửi Token JWT khi test API
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

            // Cấu hình để Swagger đọc các comment /// <summary> từ code
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Middleware để hiển thị giao diện Swagger UI
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider provider
    )
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Hiển thị UI cho từng phiên bản API (v1, v2...)
            foreach (var description in provider.ApiVersionDescriptions)
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                var name = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint(url, name);
            }
        });
        return app;
    }
}
