using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation.AspNetCore;
using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Services;
using MediatR;
using System.Reflection;

namespace FoodHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // T? d?ng quét và dang ký t?t c? các Profile c?a AutoMapper trong Assembly này
            services.AddAutoMapper(config =>
            {
                config.AddProfile<MappingProfile>();
            });



            // Ðang ký MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Ðang ký FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Ðang ký Application Services
            services.AddScoped<IEmployeeServices, EmployeeServices>();
            services.AddScoped<IMessageService, MessageService>();

            return services;
        }
    }
}
