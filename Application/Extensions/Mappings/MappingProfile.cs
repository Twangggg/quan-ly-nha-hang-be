using AutoMapper;
using System.Reflection;

namespace FoodHub.Application.Extensions.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
        }

        private void ApplyMappingsFromAssembly(Assembly assembly)
        {
            // Tìm tất cả các Type triển khai interface IMapFrom<>
            var types = assembly.GetExportedTypes()
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
                .ToList();

            foreach (var type in types)
            {
                // Lấy instance của DTO (hoặc gọi từ type)
                var instance = Activator.CreateInstance(type);

                // Tìm phương thức "Mapping" trong interface hoặc lớp
                var methodInfo = type.GetMethod("Mapping") 
                                 ?? type.GetInterface("IMapFrom`1")?.GetMethod("Mapping");

                // Thực thi phương thức Mapping để đăng ký cấu hình với AutoMapper
                methodInfo?.Invoke(instance, new object[] { this });
            }
        }
    }
}
