using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces.Security
{
    public interface IPermissionProvider
    {
        IEnumerable<string> GetPermissionsByRole(EmployeeRole role);
    }
}
