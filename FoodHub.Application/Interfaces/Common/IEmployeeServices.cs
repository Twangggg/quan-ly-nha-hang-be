using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces.Common
{
    public interface IEmployeeServices
    {
        public Task<string> GenerateEmployeeCodeAsync(EmployeeRole role);
    }
}
