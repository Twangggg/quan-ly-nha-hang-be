namespace FoodHub.Application.Common.Constants;

public static class CacheKey
{
    // ==================== CATEGORIES ====================

    /// <summary>
    /// Key cho danh sách tất cả categories
    /// Example: "category:list:all"
    /// </summary>
    public const string CategoryList = "category:list:all";

    /// <summary>
    /// Key cho một category cụ thể theo ID
    /// Example: string.Format(CategoryById, "123") → "category:123"
    /// </summary>
    public const string CategoryById = "category:{0}";

    /// <summary>
    /// Key cho danh sách categories theo type
    /// Example: string.Format(CategoryListByType, "FOOD") → "category:list:type:FOOD"
    /// </summary>
    public const string CategoryListByType = "category:list:type:{0}";
    // ==================== MENU ITEMS ====================

    /// <summary>
    /// Key cho danh sách menu items (base key)
    /// Sẽ kết hợp với filters/pagination
    /// </summary>
    public const string MenuItemList = "menuitem:list";

    /// <summary>
    /// Key cho một menu item cụ thể
    /// Example: string.Format(MenuItemById, "456") → "menuitem:456"
    /// </summary>
    public const string MenuItemById = "menuitem:{0}";

    /// <summary>
    /// Key cho menu items theo category
    /// Example: string.Format(MenuItemListByCategory, "123") → "menuitem:list:category:123"
    /// </summary>
    public const string MenuItemListByCategory = "menuitem:list:category:{0}";
    // ==================== EMPLOYEES ====================

    public const string EmployeeList = "employee:list";
    public const string EmployeeById = "employee:{0}";
    public const string EmployeeByEmail = "employee:email:{0}";

    // ==================== SET MENUS ====================

    /// <summary>
    /// Key cho danh sách set menus (base key)
    /// Sẽ kết hợp với filters/pagination
    /// </summary>
    public const string SetMenuList = "setmenu:list";

    /// <summary>
    /// Key cho một set menu cụ thể
    /// Example: string.Format(SetMenuById, "789") → "setmenu:789"
    /// </summary>
    public const string SetMenuById = "setmenu:{0}";

    // ==================== OPTIONS ====================

    /// <summary>
    /// Key cho option groups theo menu item
    /// Example: string.Format(OptionGroupsByMenuItem, "456") → "option:menuitem:456"
    /// </summary>
    public const string OptionGroupsByMenuItem = "option:menuitem:{0}";
}

public static class CacheTTL
{
    public static readonly TimeSpan Categories = TimeSpan.FromHours(2);
    public static readonly TimeSpan MenuItems = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Employees = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan SetMenus = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Options = TimeSpan.FromMinutes(30);
}
