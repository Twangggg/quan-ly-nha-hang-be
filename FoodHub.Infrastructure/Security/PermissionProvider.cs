using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
                Permissions.Tables.View,
                Permissions.Areas.View,
                Permissions.Reservations.CheckIn,
                Permissions.Billing.PreCheckBill,

                // Shift permissions for waiters
                Permissions.ShiftAssignments.ViewMyShifts,
            };
        }

        private IEnumerable<string> GetCashierPermissions()
        {
            return new List<string>
            {
                Permissions.Orders.View,
                Permissions.Orders.Complete,
                Permissions.Orders.Create,
                Permissions.Orders.Update,
                Permissions.Orders.SubmitToKitchen,
                Permissions.Orders.Split,
                Permissions.Orders.Merge,
                Permissions.Orders.ChangeTable,
                Permissions.MenuItems.View,
                Permissions.Categories.View,
                Permissions.SetMenus.View,
                Permissions.Tables.View,
                Permissions.Areas.View,
                Permissions.Billing.Checkout,
                Permissions.Billing.ViewHistory,
                Permissions.Billing.PreCheckBill,
                Permissions.Billing.SplitBill,
                Permissions.Reservations.View,
                Permissions.Reservations.Create,
                Permissions.Reservations.Update,
                Permissions.Reservations.Cancel,
                Permissions.Reservations.CheckIn,
                Permissions.SalesAnalytics.View,

                // Invoice permissions for cashiers
                Permissions.Invoices.View,
                Permissions.Invoices.ViewPdf,
                Permissions.Invoices.Create,

                // Voucher permissions for cashiers
                Permissions.Vouchers.View,
                Permissions.Vouchers.Create,
                Permissions.Vouchers.Update,
                Permissions.Vouchers.Delete,
                Permissions.Vouchers.Apply,
                Permissions.Vouchers.Unapply,
                
                // Shift permissions for cashiers
                Permissions.ShiftAssignments.ViewMyShifts,
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
                Permissions.Kds.View,
                Permissions.Kds.Manage,
                Permissions.Kds.Reject,

                // Shift permissions for chef/bar
                Permissions.ShiftAssignments.ViewMyShifts,
            };
        }
    }
}
