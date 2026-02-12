using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces
{
    public interface IPermissionProvider
    {
        IEnumerable<string> GetPermissionsByRole(EmployeeRole role);
    }
}
