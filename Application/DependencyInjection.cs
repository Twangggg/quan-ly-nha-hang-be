using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using FoodHub.Application.Common.Behaviors;

namespace FoodHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Tự động quét và đăng ký tất cả các Profile của AutoMapper trong Assembly này
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // Đăng ký MediatR
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Đăng ký FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            return services;
        }
    }
}
