using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FoodHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Tự động quét và đăng ký tất cả các Profile của AutoMapper trong Assembly này
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // Register Application Services here (e.g. MediatR, AutoMapper, Validators)
            
            return services;
        }
    }
}
