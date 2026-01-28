using FluentValidation.AspNetCore;
using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Extensions.Mappings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FoodHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Tự động quét và đăng ký tất cả các Profile của AutoMapper trong Assembly này
            services.AddAutoMapper(config =>
            {
                config.AddProfile<MappingProfile>();
            });

            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));

            // Đăng ký MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Đăng ký FluentValidation
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
