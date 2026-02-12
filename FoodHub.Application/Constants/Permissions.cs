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
    }
}
