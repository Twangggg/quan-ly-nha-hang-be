using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FoodHub.Application.Services
{
    public class EmployeeServices : IEmployeeServices
    {
        private readonly IUnitOfWork _unitOfWork;

        public EmployeeServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateEmployeeCodeAsync(EmployeeRole role)
        {
            char prefix = role switch
            {
                EmployeeRole.Manager => 'M',
                EmployeeRole.Cashier => 'C',
                EmployeeRole.Waiter => 'W',
                EmployeeRole.ChefBar => 'B',
                _ => 'U'
            };

            var currentCount = await _unitOfWork.Repository<Employee>()
                .Query()
                .CountAsync(e => e.Role == role);
            int nextNumber = currentCount + 1;

            // Format "D6" sẽ biến số 1 thành "000001"
            return $"{prefix}{nextNumber.ToString("D6")}";
        }
    }
}
