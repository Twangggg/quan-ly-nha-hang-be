using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;

namespace FoodHub.Infrastructure.Security
{
    public class PermissionProvider : IPermissionProvider
    {
        public IEnumerable<string> GetPermissionsByRole(EmployeeRole role)
        {
            return role switch
            {
                EmployeeRole.Manager => GetAllPermissions(),
                EmployeeRole.Waiter => GetWaiterPermissions(),
                EmployeeRole.Cashier => GetCashierPermissions(),
                EmployeeRole.ChefBar => GetChefBarPermissions(),
                _ => Enumerable.Empty<string>(),
            };
        }

        private IEnumerable<string> GetAllPermissions()
        {
            // Manager has all permissions
            return typeof(Permissions)
                .GetNestedTypes()
                .SelectMany(t =>
                    t.GetFields(
                        System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.Static
                            | System.Reflection.BindingFlags.FlattenHierarchy
                    )
                )
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.GetValue(null)?.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s));
        }

        private IEnumerable<string> GetWaiterPermissions()
        {
            return new List<string>
            {
                Permissions.Orders.View,
                Permissions.Orders.Create,
                Permissions.Orders.Update,
                Permissions.Orders.SubmitToKitchen,
                Permissions.MenuItems.View,
                Permissions.Categories.View,
                Permissions.SetMenus.View,
            };
        }

        private IEnumerable<string> GetCashierPermissions()
        {
            return new List<string>
            {
                Permissions.Orders.View,
                Permissions.Orders.Complete,
                Permissions.MenuItems.View,
                Permissions.Categories.View,
                Permissions.SetMenus.View,
            };
        }

        private IEnumerable<string> GetChefBarPermissions()
        {
            return new List<string>
            {
                Permissions.Orders.View,
                Permissions.MenuItems.View,
                Permissions.MenuItems.UpdateStock,
                Permissions.SetMenus.View,
                Permissions.SetMenus.UpdateStock,
            };
        }
    }
}
