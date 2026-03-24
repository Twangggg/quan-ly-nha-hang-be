namespace FoodHub.Application.Constants
{
    public static class Permissions
    {
        public static class Orders
        {
            public const string View = "Permissions.Orders.View";
            public const string Create = "Permissions.Orders.Create";
            public const string Update = "Permissions.Orders.Update";
            public const string Cancel = "Permissions.Orders.Cancel";
            public const string Complete = "Permissions.Orders.Complete";
            public const string SubmitToKitchen = "Permissions.Orders.SubmitToKitchen";
            public const string ChangeTable = "Permissions.Orders.ChangeTable";
            public const string Merge = "Permissions.Orders.Merge";
            public const string Split = "Permissions.Orders.Split";
        }

        public static class MenuItems
        {
            public const string View = "Permissions.MenuItems.View";
            public const string Create = "Permissions.MenuItems.Create";
            public const string Update = "Permissions.MenuItems.Update";
            public const string Delete = "Permissions.MenuItems.Delete";
            public const string UpdateStock = "Permissions.MenuItems.UpdateStock";
            public const string UpdateOptions = "Permissions.MenuItems.UpdateOptions";
        }

        public static class Categories
        {
            public const string View = "Permissions.Categories.View";
            public const string Create = "Permissions.Categories.Create";
            public const string Update = "Permissions.Categories.Update";
            public const string Delete = "Permissions.Categories.Delete";
        }

        public static class Employees
        {
            public const string View = "Permissions.Employees.View";
            public const string Create = "Permissions.Employees.Create";
            public const string Update = "Permissions.Employees.Update";
            public const string Delete = "Permissions.Employees.Delete";
            public const string ChangeRole = "Permissions.Employees.ChangeRole";
            public const string ViewAuditLogs = "Permissions.Employees.ViewAuditLogs";
        }

        public static class SetMenus
        {
            public const string View = "Permissions.SetMenus.View";
            public const string Create = "Permissions.SetMenus.Create";
            public const string Update = "Permissions.SetMenus.Update";
            public const string Delete = "Permissions.SetMenus.Delete";
            public const string UpdateStock = "Permissions.SetMenus.UpdateStock";
        }

        public static class Billing
        {
            public const string Checkout = "Permissions.Billing.Checkout";
            public const string ViewHistory = "Permissions.Billing.ViewHistory";
            public const string PreCheckBill = "Permissions.Billing.PreCheckBill";
            public const string SplitBill = "Permissions.Billing.SplitBill";
        }

        public static class Kds
        {
            public const string View = "Permissions.Kds.View";
            public const string Manage = "Permissions.Kds.Manage";
            public const string Reject = "Permissions.Kds.Reject";
            public const string Return = "Permissions.Kds.Return";
        }

        public static class Tables
        {
            public const string View = "Permissions.Tables.View";
            public const string Create = "Permissions.Tables.Create";
            public const string Update = "Permissions.Tables.Update";
            public const string UpdateStatus = "Permissions.Tables.UpdateStatus";
            public const string Delete = "Permissions.Tables.Delete";
        }

        public static class Areas
        {
            public const string View = "Permissions.Areas.View";
            public const string Create = "Permissions.Areas.Create";
            public const string Update = "Permissions.Areas.Update";
            public const string Delete = "Permissions.Areas.Delete";
        }

        public static class Reservations
        {
            public const string CheckIn = "Permissions.Reservations.CheckIn";
        }
        public static class SalesAnalytics
        {
            public const string View = "Permissions.SalesAnalytics.View";
        }

        public static class Inventory
        {
            public const string View = "Permissions.Inventory.View";
            public const string Create = "Permissions.Inventory.Create";
            public const string Update = "Permissions.Inventory.Update";
            public const string Deactivate = "Permissions.Inventory.Deactivate";
        }

        public static class Invoices
        {
            public const string View = "Permissions.Invoices.View";
            public const string Create = "Permissions.Invoices.Create";
            public const string ViewPdf = "Permissions.Invoices.ViewPdf";
        }

        public static class Vouchers
        {
            public const string View = "Permissions.Vouchers.View";
            public const string Create = "Permissions.Vouchers.Create";
            public const string Update = "Permissions.Vouchers.Update";
            public const string Delete = "Permissions.Vouchers.Delete";
            public const string Apply = "Permissions.Vouchers.Apply";
        }

        public static class Shifts
        {
            public const string View = "Permissions.Shifts.View";
            public const string Create = "Permissions.Shifts.Create";
            public const string Update = "Permissions.Shifts.Update";
            public const string Deactivate = "Permissions.Shifts.Deactivate";
        }

        public static class ShiftAssignments
        {
            public const string View = "Permissions.ShiftAssignments.View";
            public const string Create = "Permissions.ShiftAssignments.Create";
            public const string Update = "Permissions.ShiftAssignments.Update";
            public const string Delete = "Permissions.ShiftAssignments.Delete";
            public const string ViewMyShifts = "Permissions.Shifts.ViewMyShifts";
        }
    }
}
