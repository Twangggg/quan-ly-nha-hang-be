using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
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
                EmployeeRole.Admin => GetAllPermissions(),
                EmployeeRole.Manager => GetManagerPermissions(),
                EmployeeRole.Cashier => GetCashierPermissions(),
                EmployeeRole.ChefBar => GetChefBarPermissions(),
                _ => Enumerable.Empty<string>(),
            };
        }

        private IEnumerable<string> GetManagerPermissions()
        {
            return new List<string>
            {
                Permissions.Tables.View,
                Permissions.Tables.Create,
                Permissions.Tables.Update,
                Permissions.Tables.UpdateStatus,
                Permissions.Tables.Delete,
                Permissions.Areas.View,
                Permissions.Areas.Create,
                Permissions.Areas.Update,
                Permissions.Areas.Delete,
                Permissions.Orders.View,
                Permissions.Orders.Create,
                Permissions.Orders.Update,
                Permissions.Orders.Cancel,
                Permissions.Orders.Complete,
                Permissions.Orders.SubmitToKitchen,
                Permissions.Orders.ChangeTable,
                Permissions.Orders.Merge,
                Permissions.Orders.Split,
                Permissions.Kds.View,
                Permissions.Kds.Manage,
                Permissions.Kds.Reject,
                Permissions.Kds.Return,
                Permissions.Billing.Checkout,
                Permissions.Billing.ViewHistory,
                Permissions.Billing.PreCheckBill,
                Permissions.Billing.SplitBill,
                Permissions.Invoices.View,
                Permissions.Invoices.Create,
                Permissions.Invoices.ViewPdf,
                Permissions.PaymentMethods.View,
                Permissions.Vouchers.View,
                Permissions.Vouchers.Create,
                Permissions.Vouchers.Update,
                Permissions.Vouchers.Delete,
                Permissions.Vouchers.UpdateStatus,
                Permissions.Vouchers.Apply,
                Permissions.Vouchers.Unapply,
                Permissions.Shifts.View,
                Permissions.Shifts.Create,
                Permissions.Shifts.Update,
                Permissions.Shifts.Deactivate,
                Permissions.ShiftAssignments.View,
                Permissions.ShiftAssignments.Create,
                Permissions.ShiftAssignments.Update,
                Permissions.ShiftAssignments.Delete,
                Permissions.ShiftAssignments.ViewMyShifts,
                Permissions.Attendances.View,
                Permissions.Attendances.CheckIn,
                Permissions.Attendances.CheckOut,
                Permissions.Images.Manage,
                Permissions.Reservations.View,
                Permissions.Reservations.Create,
                Permissions.Reservations.Update,
                Permissions.Reservations.Cancel,
                Permissions.Reservations.CheckIn,
                Permissions.MenuItems.View,
                Permissions.Categories.View,
                Permissions.SetMenus.View,
                Permissions.Employees.View
            };
        }

        private IEnumerable<string> GetAllPermissions()
        {
            // Admin has all permissions
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

        private IEnumerable<string> GetCashierPermissions()
        {
            return new List<string>
            {
                Permissions.Orders.View,
                Permissions.Orders.Complete,
                Permissions.Orders.Create,
                Permissions.Orders.Cancel,
                Permissions.Orders.Update,
                Permissions.Orders.Cancel,
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
                Permissions.PaymentMethods.View,
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
                // Check In/Out permissions for cashiers
                Permissions.Attendances.CheckIn,
                Permissions.Attendances.CheckOut,
                // Image permissions for branding/avatar
                Permissions.Images.Manage,
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
                // Inventory permissions for chef/bar
                Permissions.Inventory.View,
                Permissions.Inventory.Create,
                Permissions.Inventory.Update,
                Permissions.Inventory.Deactivate,
                Permissions.Inventory.Import,
                // Shift permissions for chef/bar
                Permissions.ShiftAssignments.ViewMyShifts,
                // Check In/Out permissions for chef/bar
                Permissions.Attendances.CheckIn,
                Permissions.Attendances.CheckOut,
            };
        }
    }
}
