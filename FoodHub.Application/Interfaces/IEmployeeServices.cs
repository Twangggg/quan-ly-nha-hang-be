using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces
{
    public interface IEmployeeServices
    {
        public Task<string> GenerateEmployeeCodeAsync(EmployeeRole role);
    }
}
