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

            var lastEmployee = await _unitOfWork.Repository<Employee>()
                .Query()
                .Where(e => e.Role == role)
                .OrderByDescending(e => e.EmployeeCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEmployee != null && !string.IsNullOrEmpty(lastEmployee.EmployeeCode) && lastEmployee.EmployeeCode.Length > 1)
            {
                if (int.TryParse(lastEmployee.EmployeeCode.Substring(1), out int lastId))
                {
                    nextNumber = lastId + 1;
                }
            }

            // Format "D6" s? bi?n s? 1 thành "000001"
            return $"{prefix}{nextNumber.ToString("D6")}";
        }
    }
}
