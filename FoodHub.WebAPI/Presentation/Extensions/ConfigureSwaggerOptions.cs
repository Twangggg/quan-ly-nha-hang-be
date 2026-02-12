using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodHub.WebAPI.Presentation.Extensions
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) =>
            _provider = provider;

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        }

        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = "FoodHub API",
                Version = description.ApiVersion.ToString(),
                Description = "API cho hệ thống quản lý nhà hàng FoodHub.",
            };

            if (description.IsDeprecated)
            {
                info.Description += " (Phiên bản này đã lỗi thời).";
            }

            return info;
        }
    }
}
