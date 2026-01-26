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

            // Đăng ký MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Đăng ký FluentValidation
            // Lưu ý: Cần cài đặt FluentValidation.DependencyInjectionExtensions nếu chưa có
            // Ở đây đã cài FluentValidation.AspNetCore nên có thể dùng trực tiếp hoặc qua Scan
            
            return services;
        }
    }
}
