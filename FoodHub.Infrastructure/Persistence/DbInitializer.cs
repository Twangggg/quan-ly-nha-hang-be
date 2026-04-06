using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence
{
    public class DbInitializer
    {
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;

        public DbInitializer(AppDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public void Initialize()
        {
            // Apply pending migrations first.
            if (_context.Database.GetPendingMigrations().Any())
            {
                _context.Database.Migrate();
            }

            RepairKnownSchemaDrift();

            // Seed Data
            if (!_context.Employees.Any())
            {
                var employees = new Employee[]
                {
                    new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        EmployeeCode = "M000001",
                        Username = "admin",
                        PasswordHash = _passwordService.HashPassword("New123!!"),
                        FullName = "Admin Manager",
                        Email = "liem20052012@gmail.com",
                        Phone = "0909000001",
                        Role = EmployeeRole.Manager,
                        Status = EmployeeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        EmployeeCode = "B000001",
                        Username = "chef",
                        PasswordHash = _passwordService.HashPassword("New123!!"),
                        FullName = "Chief Chef",
                        Email = "chef@foodhub.com",
                        Phone = "0909000002",
                        Role = EmployeeRole.ChefBar,
                        Status = EmployeeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        EmployeeCode = "C000001",
                        Username = "cashier",
                        PasswordHash = _passwordService.HashPassword("New123!!"),
                        FullName = "Cashier One",
                        Email = "cashier@foodhub.com",
                        Phone = "0909000004",
                        Role = EmployeeRole.Cashier,
                        Status = EmployeeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                };

                foreach (var e in employees)
                {
                    // Check if already exists to avoid duplicate key errors
                    if (
                        !_context.Employees.Any(x =>
                            x.EmployeeCode == e.EmployeeCode
                            || x.Username == e.Username
                            || x.Email == e.Email
                        )
                    )
                    {
                        _context.Employees.Add(e);
                    }
                }
                _context.SaveChanges();
            }

            if (!_context.Categories.Any() && !_context.MenuItems.Any())
            {
                var appCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Khai vị",
                    CodePrefix = "APP",
                    CategoryType = CategoryType.Normal,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                var mainCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Món chính",
                    CodePrefix = "MAIN",
                    CategoryType = CategoryType.Normal,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                var drinkCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Đồ uống",
                    CodePrefix = "DRK",
                    CategoryType = CategoryType.Normal,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                var dessertCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Tráng miệng",
                    CodePrefix = "DES",
                    CategoryType = CategoryType.Normal,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                var comboCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Combo",
                    CodePrefix = "COMBO",
                    CategoryType = CategoryType.Combo,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                _context.Categories.AddRange(
                    appCategory,
                    mainCategory,
                    drinkCategory,
                    dessertCategory,
                    comboCategory
                );
                _context.SaveChanges();

                var menuItems = new List<MenuItem>
                {
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "APP-001",
                        ItemNumber = 1,
                        Name = "Chả giò tôm thịt",
                        ImageUrl = "",
                        Description = "Crispy spring rolls with shrimp and pork",
                        CategoryId = appCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 10,
                        Price = 45000,
                        CostPrice = 20000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "MAIN-001",
                        ItemNumber = 1,
                        Name = "Cơm gà xối mỡ",
                        ImageUrl = "",
                        Description = "Fried chicken rice",
                        CategoryId = mainCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 15,
                        Price = 55000,
                        CostPrice = 30000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "MAIN-002",
                        ItemNumber = 2,
                        Name = "Phở bò truyền thống",
                        ImageUrl = "",
                        Description = "Traditional beef noodle soup",
                        CategoryId = mainCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 12,
                        Price = 65000,
                        CostPrice = 35000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "DRK-001",
                        ItemNumber = 1,
                        Name = "Cocktail đặc biệt",
                        ImageUrl = "",
                        Description = "Signature house cocktail",
                        CategoryId = drinkCategory.CategoryId,
                        Station = Station.Bar,
                        ExpectedTime = 5,
                        Price = 75000,
                        CostPrice = 30000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "DES-001",
                        ItemNumber = 1,
                        Name = "Chè khúc bạch",
                        ImageUrl = "",
                        Description = "Milk jelly with lychee",
                        CategoryId = dessertCategory.CategoryId,
                        Station = Station.Bar,
                        ExpectedTime = 3,
                        Price = 35000,
                        CostPrice = 15000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.Parse("84b3ff00-82f6-4e52-8d2b-c669cc2524bd"),
                        Code = "COMBO-001",
                        ItemNumber = 1,
                        Name = "Combo Ấm Áp",
                        ImageUrl = "",
                        Description = "Cơm gà xối mỡ + Cocktail đặc biệt + Chè khúc bạch",
                        CategoryId = comboCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 20,
                        Price = 180000,
                        CostPrice = 95000,
                        CreatedAt = DateTime.UtcNow,
                    },
                };

                _context.MenuItems.AddRange(menuItems);
                _context.SaveChanges();
            }

            if (!_context.SetMenus.Any())
            {
                var comboCategory = _context.Categories.FirstOrDefault(c => c.Name == "Combo");

                if (comboCategory != null)
                {
                    var setMenu1 = new SetMenu
                    {
                        SetMenuId = Guid.NewGuid(),
                        Code = "COMBO-001",
                        ItemNumber = 1,
                        CategoryId = comboCategory.CategoryId,
                        Name = "Combo Ăn Trưa",
                        Price = 99000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow,
                    };

                    var setMenu2 = new SetMenu
                    {
                        SetMenuId = Guid.NewGuid(),
                        Code = "COMBO-002",
                        ItemNumber = 2,
                        CategoryId = comboCategory.CategoryId,
                        Name = "Combo Gia Đình",
                        Price = 250000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.SetMenus.AddRange(setMenu1, setMenu2);
                    _context.SaveChanges();

                    // Add some items to the first combo
                    var chickenRice = _context.MenuItems.FirstOrDefault(mi =>
                        mi.Code == "MAIN-001"
                    );
                    var specialDrink = _context.MenuItems.FirstOrDefault(mi =>
                        mi.Code == "DRK-001"
                    );
                    var dessert = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DES-001");

                    if (chickenRice != null && specialDrink != null)
                    {
                        var setMenuItem1 = new SetMenuItem
                        {
                            SetMenuItemId = Guid.NewGuid(),
                            SetMenuId = setMenu1.SetMenuId,
                            MenuItemId = chickenRice.MenuItemId,
                            Quantity = 1,
                            CreatedAt = DateTime.UtcNow,
                        };

                        var setMenuItem2 = new SetMenuItem
                        {
                            SetMenuItemId = Guid.NewGuid(),
                            SetMenuId = setMenu1.SetMenuId,
                            MenuItemId = specialDrink.MenuItemId,
                            Quantity = 1,
                            CreatedAt = DateTime.UtcNow,
                        };

                        _context.SetMenuItems.AddRange(setMenuItem1, setMenuItem2);
                        _context.SaveChanges();
                    }

                    // Add a third combo referencing the new COMBO-001 menu item
                    var comboMenuItem = _context.MenuItems.FirstOrDefault(mi =>
                        mi.Code == "COMBO-001"
                    );

                    if (
                        comboMenuItem != null
                        && chickenRice != null
                        && specialDrink != null
                        && dessert != null
                    )
                    {
                        var setMenu3 = new SetMenu
                        {
                            SetMenuId = Guid.NewGuid(),
                            Code = "COMBO-003",
                            ItemNumber = 3,
                            CategoryId = comboCategory.CategoryId,
                            Name = "Combo Ấm Áp",
                            Price = 180000,
                            IsOutOfStock = false,
                            CreatedAt = DateTime.UtcNow,
                        };

                        _context.SetMenus.Add(setMenu3);
                        _context.SaveChanges();

                        var setMenuItemA = new SetMenuItem
                        {
                            SetMenuItemId = Guid.NewGuid(),
                            SetMenuId = setMenu3.SetMenuId,
                            MenuItemId = chickenRice.MenuItemId,
                            Quantity = 1,
                            CreatedAt = DateTime.UtcNow,
                        };

                        var setMenuItemB = new SetMenuItem
                        {
                            SetMenuItemId = Guid.NewGuid(),
                            SetMenuId = setMenu3.SetMenuId,
                            MenuItemId = specialDrink.MenuItemId,
                            Quantity = 1,
                            CreatedAt = DateTime.UtcNow,
                        };

                        var setMenuItemC = new SetMenuItem
                        {
                            SetMenuItemId = Guid.NewGuid(),
                            SetMenuId = setMenu3.SetMenuId,
                            MenuItemId = dessert.MenuItemId,
                            Quantity = 1,
                            CreatedAt = DateTime.UtcNow,
                        };

                        _context.SetMenuItems.AddRange(setMenuItemA, setMenuItemB, setMenuItemC);
                        _context.SaveChanges();
                    }
                }
            }

            // Seed Inventory Groups
            if (!_context.InventoryGroups.Any())
            {
                var groups = new[]
                {
                    InventoryGroup.Create(
                        "Thực phẩm tươi sống",
                        "Các loại thịt, cá, hải sản tươi",
                        10m,
                        2,
                        InventoryCostMethod.WeightedAverage
                    ),
                    InventoryGroup.Create(
                        "Rau củ quả",
                        "Các loại rau, củ, trái cây bảo quản lạnh",
                        5m,
                        3,
                        InventoryCostMethod.WeightedAverage
                    ),
                    InventoryGroup.Create(
                        "Gia vị & Đồ khô",
                        "Muối, đường, hạt nêm, đồ đóng hộp",
                        15m,
                        null,
                        InventoryCostMethod.WeightedAverage
                    ),
                    InventoryGroup.Create(
                        "Đồ uống",
                        "Nước ngọt, bia, rượu, sữa",
                        20m,
                        30,
                        InventoryCostMethod.WeightedAverage
                    ),
                };
                _context.InventoryGroups.AddRange(groups);
                _context.SaveChanges();
            }

            // Seed Ingredients for Inventory module
            if (!_context.Ingredients.Any())
            {
                var freshFoodGroup = _context.InventoryGroups.FirstOrDefault(g =>
                    g.Name == "Thực phẩm tươi sống"
                );
                var vegetableGroup = _context.InventoryGroups.FirstOrDefault(g =>
                    g.Name == "Rau củ quả"
                );
                var drinkGroup = _context.InventoryGroups.FirstOrDefault(g => g.Name == "Đồ uống");

                var seedIngredients = new[]
                {
                    new
                    {
                        Code = "THITBO-1",
                        Name = "Thịt bò",
                        Unit = "kg",
                        LowStockThreshold = 10m,
                        Description = "Thịt bò tươi cho món chính",
                        Stock = 0m,
                        Cost = 0m,
                        GroupId = freshFoodGroup?.InventoryGroupId,
                    },
                    new
                    {
                        Code = "UCGA-2",
                        Name = "Ức gà",
                        Unit = "kg",
                        LowStockThreshold = 12m,
                        Description = "Ức gà fillet không da",
                        Stock = 0m,
                        Cost = 0m,
                        GroupId = freshFoodGroup?.InventoryGroupId,
                    },
                    new
                    {
                        Code = "RAUXALACH-3",
                        Name = "Rau xà lách",
                        Unit = "kg",
                        LowStockThreshold = 5m,
                        Description = "Rau xà lách Đà Lạt",
                        Stock = 0m,
                        Cost = 0m,
                        GroupId = vegetableGroup?.InventoryGroupId,
                    },
                    new
                    {
                        Code = "SUATUOI-4",
                        Name = "Sữa tươi",
                        Unit = "l",
                        LowStockThreshold = 20m,
                        Description = "Sữa tươi tiệt trùng",
                        Stock = 0m,
                        Cost = 0m,
                        GroupId = drinkGroup?.InventoryGroupId,
                    },
                };

                var ingredients = new List<Ingredient>();

                foreach (var seed in seedIngredients)
                {
                    var ingredient = Ingredient.Create(
                        seed.Code,
                        seed.Name,
                        seed.Unit,
                        seed.LowStockThreshold,
                        seed.Stock,
                        seed.Cost,
                        seed.Description,
                        inventoryGroupId: seed.GroupId
                    );

                    ingredient.UpdateStock(seed.Stock, seed.Cost);
                    ingredients.Add(ingredient);
                }

                _context.Ingredients.AddRange(ingredients);
                _context.SaveChanges();
            }

            // Ensure the tables and areas exist before seeding orders, since orders reference tables
            if (!_context.Areas.Any())
            {
                var areas = new Area[]
                {
                    new Area
                    {
                        AreaId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Name = "Indoor",
                        CodePrefix = "F1",
                        Type = AreaType.Normal,
                        Description = "General indoor dining area",
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Area
                    {
                        AreaId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Name = "Outdoor",
                        CodePrefix = "F2",
                        Type = AreaType.Normal,
                        Description = "Outdoor seating area",
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Area
                    {
                        AreaId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                        Name = "Phòng VIP 1",
                        CodePrefix = "VIP1",
                        Type = AreaType.VIP,
                        Description = "Phòng riêng VIP 1 - Sức chứa lớn",
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Area
                    {
                        AreaId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                        Name = "Phòng VIP 2",
                        CodePrefix = "VIP2",
                        Type = AreaType.VIP,
                        Description = "Phòng riêng VIP 2 - Sức chứa lớn",
                        CreatedAt = DateTime.UtcNow,
                    },
                };
                _context.Areas.AddRange(areas);
                _context.SaveChanges();
            }

            // Seed tables with specific IDs to match FE expectations (12 tables)
            for (int i = 1; i <= 12; i++)
            {
                var tableId = Guid.Parse($"00000000-0000-0000-0000-0000000000{i:D2}");
                if (!_context.Tables.Any(t => t.TableId == tableId))
                {
                    Guid areaId;
                    int capacity;

                    if (i <= 8)
                    {
                        areaId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                        capacity = (i % 2 == 0 ? 4 : 2);
                    }
                    else if (i <= 10)
                    {
                        areaId = Guid.Parse("00000000-0000-0000-0000-000000000002");
                        capacity = 4;
                    }
                    else if (i == 11)
                    {
                        areaId = Guid.Parse("00000000-0000-0000-0000-000000000003");
                        capacity = 100; // Phòng VIP có thể chứa rất nhiều khách
                    }
                    else // i == 12
                    {
                        areaId = Guid.Parse("00000000-0000-0000-0000-000000000004");
                        capacity = 100; // Phòng VIP có thể chứa rất nhiều khách
                    }

                    _context.Tables.Add(
                        new Table
                        {
                            TableId = tableId,
                            TableNumber = i,
                            Capacity = capacity,
                            AreaId = areaId,
                            Status = TableStatus.Available,
                            CreatedAt = DateTime.UtcNow,
                        }
                    );
                }
            }
            _context.SaveChanges();

            // Seed default PaymentMethodConfigs - required for checkout to work
            if (!_context.PaymentMethodConfigs.Any())
            {
                var cashConfig = new PaymentMethodConfig
                {
                    PaymentMethodConfigId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "Tiền mặt",
                    Type = PaymentMethodType.Cash,
                    IsActive = true,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow,
                };

                var bankConfig = new PaymentMethodConfig
                {
                    PaymentMethodConfigId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Name = "Chuyển khoản",
                    Type = PaymentMethodType.BankTransfer,
                    IsActive = true,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.PaymentMethodConfigs.AddRange(cashConfig, bankConfig);
                _context.SaveChanges();
            }

            if (!_context.Orders.Any())
            {
                var admin = _context.Employees.FirstOrDefault(e => e.EmployeeCode == "M001001");
                var chickenRice = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-001");
                var beefNoodle = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-002");
                var specialDrink = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-001");
                if (
                    admin == null
                    || chickenRice == null
                    || beefNoodle == null
                    || specialDrink == null
                )
                {
                    // Basic dependencies are missing, skip seeding orders as it depends on these specific items
                    return;
                }

                if (admin != null && chickenRice != null && beefNoodle != null && specialDrink != null)
                {
                    // Table IDs that match FE expectation (ending with 01, 02)
                    var table01Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
                    var table02Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

                    var order1 = new Order
                    {
                        OrderId = Guid.NewGuid(),
                        OrderCode = $"ORD-{DateTime.Now:yyyyMMdd}-0001",
                        OrderType = OrderType.DineIn,
                        Status = OrderStatus.Serving,
                        TableId = table02Id,
                        TotalAmount = chickenRice.Price + specialDrink.Price,
                        CreatedByEmployee = admin,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                    };

                    order1.OrderItems.Add(
                        new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order1.OrderId,
                            MenuItemId = chickenRice.MenuItemId,
                            ItemCodeSnapshot = chickenRice.Code,
                            ItemNameSnapshot = chickenRice.Name,
                            StationSnapshot = chickenRice.Station.ToString(),
                            Status = OrderItemStatus.Completed,
                            Quantity = 1,
                            UnitPriceSnapshot = chickenRice.Price,
                            CreatedAt = order1.CreatedAt,
                        }
                    );

                    order1.OrderItems.Add(
                        new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order1.OrderId,
                            MenuItemId = specialDrink.MenuItemId,
                            ItemCodeSnapshot = specialDrink.Code,
                            ItemNameSnapshot = specialDrink.Name,
                            StationSnapshot = specialDrink.Station.ToString(),
                            Status = OrderItemStatus.Completed,
                            Quantity = 1,
                            UnitPriceSnapshot = specialDrink.Price,
                            CreatedAt = order1.CreatedAt,
                        }
                    );

                    var order2 = new Order
                    {
                        OrderId = Guid.NewGuid(),
                        OrderCode = $"ORD-{DateTime.Now:yyyyMMdd}-0002",
                        OrderType = OrderType.DineIn,
                        Status = OrderStatus.Serving,
                        TableId = table01Id,
                        TotalAmount = beefNoodle.Price,
                        CreatedByEmployee = admin,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                    };

                    order2.OrderItems.Add(
                        new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order2.OrderId,
                            MenuItemId = beefNoodle.MenuItemId,
                            ItemCodeSnapshot = beefNoodle.Code,
                            ItemNameSnapshot = beefNoodle.Name,
                            StationSnapshot = beefNoodle.Station.ToString(),
                            Status = OrderItemStatus.Preparing,
                            Quantity = 1,
                            UnitPriceSnapshot = beefNoodle.Price,
                            CreatedAt = order2.CreatedAt,
                        }
                    );

                    var order3 = new Order
                    {
                        OrderId = Guid.NewGuid(),
                        OrderCode = $"ORD-{DateTime.Now:yyyyMMdd}-0003",
                        OrderType = OrderType.Takeaway,
                        Status = OrderStatus.Serving,
                        TotalAmount = specialDrink.Price,
                        CreatedByEmployee = admin,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    };

                    order3.OrderItems.Add(
                        new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order3.OrderId,
                            MenuItemId = specialDrink.MenuItemId,
                            ItemCodeSnapshot = specialDrink.Code,
                            ItemNameSnapshot = specialDrink.Name,
                            StationSnapshot = specialDrink.Station.ToString(),
                            Status = OrderItemStatus.Completed,
                            Quantity = 1,
                            UnitPriceSnapshot = specialDrink.Price,
                            CreatedAt = order3.CreatedAt,
                        }
                    );

                    _context.Orders.AddRange(order1, order2, order3);

                    // Update Table statuses for seeded orders
                    var table1 =
                        _context.Tables.Local.FirstOrDefault(t => t.TableId == table01Id)
                        ?? _context.Tables.FirstOrDefault(t => t.TableId == table01Id);
                    var table2 =
                        _context.Tables.Local.FirstOrDefault(t => t.TableId == table02Id)
                        ?? _context.Tables.FirstOrDefault(t => t.TableId == table02Id);

                    if (table1 != null)
                        table1.Status = TableStatus.Occupied;
                    if (table2 != null)
                        table2.Status = TableStatus.Occupied;

                    _context.SaveChanges();
                }
            }

            SyncOccupiedTablesFromActiveOrders();

            // Seed an invoice for the first order to demonstrate the relationship and for FE testing
            var environmentName = System.Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT"
            );
            var isDevOrDemo =
                string.Equals(
                    environmentName,
                    "Development",
                    System.StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    environmentName,
                    "Demo",
                    System.StringComparison.OrdinalIgnoreCase
                );

            if (isDevOrDemo && !_context.Invoices.Any())
            {
                // Use the first available order (if any) without relying on hard-coded order codes/IDs
                var order1 = _context.Orders.Include(o => o.OrderItems).FirstOrDefault();
                if (order1 != null)
                {
                    var invoice1 = new Invoice
                    {
                        InvoiceId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        OrderId = order1.OrderId,
                        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-0001",
                        SubTotal = order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot),
                        TaxAmount =
                            order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot) * 0.1m, // Assuming 10% tax
                        DiscountAmount = 0m,
                        TotalAmount =
                            order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot) * 1.1m, // Subtotal + Tax
                        AmountReceived =
                            order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot) * 1.1m,
                        AmountReturned = 0m,
                        PaymentMethod = PaymentMethod.Cash,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.Invoices.Add(invoice1);
                    _context.SaveChanges();

                    if (!_context.InvoiceItems.Any(ii => ii.InvoiceId == invoice1.InvoiceId))
                    {
                        var invoiceItems = order1
                            .OrderItems.Select(oi => new InvoiceItem
                            {
                                InvoiceId = invoice1.InvoiceId,
                                ItemName = oi.ItemNameSnapshot,
                                Quantity = oi.Quantity,
                                UnitPrice = oi.UnitPriceSnapshot,
                                TotalPrice = oi.Quantity * oi.UnitPriceSnapshot,
                                Note = oi.ItemNote,
                                CreatedAt = DateTime.UtcNow,
                            })
                            .ToList();
                        _context.InvoiceItems.AddRange(invoiceItems);
                    }

                    _context.SaveChanges();
                }
            }

            if (isDevOrDemo && !_context.Promotions.Any())
            {
                var promo1 = new Promotion
                {
                    PromotionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Code = "DISCOUNT10",
                    Type = PromotionType.Percent,
                    Value = 10m,
                    MaxDiscount = 50000m,
                    MinOrderValue = 100000m,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                var drinkMenuItem = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-001");

                if (drinkMenuItem != null)
                {
                    var promo2 = new Promotion
                    {
                        PromotionId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Code = "FREEDRINK",
                        Type = PromotionType.FreeItem,
                        ItemId = drinkMenuItem.MenuItemId,
                        FreeQuantity = 1,
                        MinOrderValue = 200000m,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.Promotions.Add(promo2);
                }

                _context.Promotions.Add(promo1);
                _context.SaveChanges();
            }

            // Seed Shifts table
            if (isDevOrDemo && !_context.Shifts.Any())
            {
                var shifts = new[]
                {
                    new Shift
                    {
                        ShiftId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Name = "Ca Sáng",
                        StartTime = new TimeSpan(6, 0, 0),
                        EndTime = new TimeSpan(14, 0, 0),
                        Status = ShiftStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Shift
                    {
                        ShiftId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Name = "Ca Chiều",
                        StartTime = new TimeSpan(14, 0, 0),
                        EndTime = new TimeSpan(22, 0, 0),
                        Status = ShiftStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Shift
                    {
                        ShiftId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                        Name = "Ca Đêm",
                        StartTime = new TimeSpan(22, 0, 0),
                        EndTime = new TimeSpan(6, 0, 0),
                        Status = ShiftStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                };
                _context.Shifts.AddRange(shifts);
                _context.SaveChanges();
            }

            // Seed ShiftAssignments for operational stats
            if (isDevOrDemo && !_context.ShiftAssignments.Any())
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var allEmployees = _context
                    .Employees.Where(e => e.Status == EmployeeStatus.Active)
                    .ToList();

                foreach (var emp in allEmployees)
                {
                    _context.ShiftAssignments.Add(
                        new ShiftAssignment
                        {
                            ShiftAssignmentId = Guid.NewGuid(),
                            EmployeeId = emp.EmployeeId,
                            ShiftId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                            AssignedDate = today,
                            CreatedAt = DateTime.UtcNow.AddHours(-2),
                        }
                    );
                }
                _context.SaveChanges();
            }

            // Seed AuditLogs for audit logs API
            if (isDevOrDemo && !_context.AuditLogs.Any())
            {
                var admin = _context.Employees.FirstOrDefault(e => e.Role == EmployeeRole.Manager);
                var table01 = _context.Tables.FirstOrDefault(t => t.TableNumber == 1);

                if (admin != null)
                {
                    var auditLogs = new List<AuditLog>
                    {
                        new AuditLog
                        {
                            LogId = Guid.NewGuid(),
                            EntityName = "Table",
                            EntityId = table01?.TableId.ToString() ?? "T-01",
                            Action = AuditAction.StatusChange,
                            OldValues = "{\"status\": \"Available\"}",
                            NewValues = "{\"status\": \"Occupied\"}",
                            ActorInfo = admin.FullName,
                            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                        },
                        new AuditLog
                        {
                            LogId = Guid.NewGuid(),
                            EntityName = "Order",
                            EntityId = "ORD-001",
                            Action = AuditAction.Create,
                            NewValues = "{\"totalAmount\": 150000}",
                            ActorInfo = admin.FullName,
                            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-45),
                        },
                        new AuditLog
                        {
                            LogId = Guid.NewGuid(),
                            EntityName = "MenuItem",
                            EntityId = "MAIN-001",
                            Action = AuditAction.Update,
                            OldValues = "{\"price\": 50000}",
                            NewValues = "{\"price\": 55000}",
                            ActorInfo = admin.FullName,
                            CreatedAt = DateTimeOffset.UtcNow.AddHours(-3),
                        },
                        new AuditLog
                        {
                            LogId = Guid.NewGuid(),
                            EntityName = "Promotion",
                            EntityId = "DISCOUNT10",
                            Action = AuditAction.Activate,
                            NewValues = "{\"isActive\": true}",
                            ActorInfo = admin.FullName,
                            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                        },
                        new AuditLog
                        {
                            LogId = Guid.NewGuid(),
                            EntityName = "Employee",
                            EntityId = "C004001",
                            Action = AuditAction.Create,
                            NewValues = "{\"role\": \"Cashier\"}",
                            ActorInfo = admin.FullName,
                            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                        },
                    };

                    _context.AuditLogs.AddRange(auditLogs);
                    _context.SaveChanges();
                }
            }

            // Seed more Orders with Paid status for salesanalytics
            if (isDevOrDemo)
            {
                var paidOrdersCount = _context.Orders.Count(o =>
                    o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed
                );
                var admin = _context.Employees.FirstOrDefault(e => e.EmployeeCode == "M001001");
                var chickenRice = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-001");
                var beefNoodle = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-002");
                var springRoll = _context.MenuItems.FirstOrDefault(mi => mi.Code == "APP-001");
                var specialDrink = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-001");

                if (admin != null && chickenRice != null && paidOrdersCount == 0)
                {
                    var today = DateTime.UtcNow.Date;

                    // Orders from past 7 days for moving average calculation
                    var historicalOrders = new List<Order>();

                    // Yesterday
                    historicalOrders.Add(
                        CreatePaidOrder(admin, chickenRice, specialDrink, today.AddDays(-1), 2)
                    );
                    historicalOrders.Add(
                        CreatePaidOrder(admin, beefNoodle, null, today.AddDays(-1), 1)
                    );

                    // 2 days ago
                    historicalOrders.Add(
                        CreatePaidOrder(admin, chickenRice, null, today.AddDays(-2), 1)
                    );
                    historicalOrders.Add(
                        CreatePaidOrder(admin, springRoll, specialDrink, today.AddDays(-2), 3)
                    );

                    // 3 days ago
                    historicalOrders.Add(
                        CreatePaidOrder(admin, beefNoodle, springRoll, today.AddDays(-3), 2)
                    );

                    // 4 days ago
                    historicalOrders.Add(
                        CreatePaidOrder(admin, chickenRice, beefNoodle, today.AddDays(-4), 1)
                    );

                    // 5 days ago
                    historicalOrders.Add(
                        CreatePaidOrder(admin, specialDrink, null, today.AddDays(-5), 4)
                    );

                    _context.Orders.AddRange(historicalOrders);
                    _context.SaveChanges();
                }
            }

            // Seed OrderItems in Preparing/Cooking status for KDS backlog
            if (isDevOrDemo)
            {
                var preparingItemsCount = _context.OrderItems.Count(oi =>
                    oi.Status == OrderItemStatus.Preparing || oi.Status == OrderItemStatus.Cooking
                );

                var chickenRice = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-001");
                var beefNoodle = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-002");
                var springRoll = _context.MenuItems.FirstOrDefault(mi => mi.Code == "APP-001");
                var specialDrink = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-001");
                var dessert = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DES-001");

                var servingOrders = _context
                    .Orders.Where(o => o.Status == OrderStatus.Serving)
                    .ToList();

                if (chickenRice != null && preparingItemsCount == 0 && servingOrders.Any())
                {
                    var kdsItems = new List<OrderItem>();
                    var now = DateTime.UtcNow;

                    foreach (var order in servingOrders)
                    {
                        // Add preparing items
                        kdsItems.Add(
                            new OrderItem
                            {
                                OrderItemId = Guid.NewGuid(),
                                OrderId = order.OrderId,
                                MenuItemId = chickenRice.MenuItemId,
                                ItemCodeSnapshot = chickenRice.Code,
                                ItemNameSnapshot = chickenRice.Name,
                                StationSnapshot = Station.HotKitchen.ToString(),
                                Status = OrderItemStatus.Preparing,
                                Quantity = 1,
                                UnitPriceSnapshot = chickenRice.Price,
                                CreatedAt = now.AddMinutes(-5),
                            }
                        );

                        if (beefNoodle != null)
                        {
                            kdsItems.Add(
                                new OrderItem
                                {
                                    OrderItemId = Guid.NewGuid(),
                                    OrderId = order.OrderId,
                                    MenuItemId = beefNoodle.MenuItemId,
                                    ItemCodeSnapshot = beefNoodle.Code,
                                    ItemNameSnapshot = beefNoodle.Name,
                                    StationSnapshot = Station.HotKitchen.ToString(),
                                    Status = OrderItemStatus.Cooking,
                                    Quantity = 1,
                                    UnitPriceSnapshot = beefNoodle.Price,
                                    CreatedAt = now.AddMinutes(-15), // Delayed item
                                }
                            );
                        }
                    }

                    // Add some more preparing items from other orders
                    if (springRoll != null)
                    {
                        kdsItems.Add(
                            new OrderItem
                            {
                                OrderItemId = Guid.NewGuid(),
                                OrderId = servingOrders.First().OrderId,
                                MenuItemId = springRoll.MenuItemId,
                                ItemCodeSnapshot = springRoll.Code,
                                ItemNameSnapshot = springRoll.Name,
                                StationSnapshot = Station.HotKitchen.ToString(),
                                Status = OrderItemStatus.Preparing,
                                Quantity = 2,
                                UnitPriceSnapshot = springRoll.Price,
                                CreatedAt = now.AddMinutes(-2),
                            }
                        );
                    }

                    if (specialDrink != null)
                    {
                        kdsItems.Add(
                            new OrderItem
                            {
                                OrderItemId = Guid.NewGuid(),
                                OrderId = servingOrders.First().OrderId,
                                MenuItemId = specialDrink.MenuItemId,
                                ItemCodeSnapshot = specialDrink.Code,
                                ItemNameSnapshot = specialDrink.Name,
                                StationSnapshot = Station.Bar.ToString(),
                                Status = OrderItemStatus.Preparing,
                                Quantity = 1,
                                UnitPriceSnapshot = specialDrink.Price,
                                CreatedAt = now.AddMinutes(-1),
                            }
                        );
                    }

                    if (dessert != null)
                    {
                        kdsItems.Add(
                            new OrderItem
                            {
                                OrderItemId = Guid.NewGuid(),
                                OrderId = servingOrders.First().OrderId,
                                MenuItemId = dessert.MenuItemId,
                                ItemCodeSnapshot = dessert.Code,
                                ItemNameSnapshot = dessert.Name,
                                StationSnapshot = Station.Bar.ToString(),
                                Status = OrderItemStatus.Cooking,
                                Quantity = 1,
                                UnitPriceSnapshot = dessert.Price,
                                CreatedAt = now.AddMinutes(-25), // Delayed item
                            }
                        );
                    }

                    _context.OrderItems.AddRange(kdsItems);
                    _context.SaveChanges();
                }
            }

            _context.SaveChanges();
        }

        private Order CreatePaidOrder(
            Employee admin,
            MenuItem item1,
            MenuItem? item2,
            DateTime date,
            int quantity
        )
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = $"ORD-{date:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                OrderType = OrderType.DineIn,
                Status = OrderStatus.Paid,
                TotalAmount = (item1.Price * quantity) + (item2?.Price ?? 0),
                SubTotal = (item1.Price * quantity) + (item2?.Price ?? 0),
                VatAmount = ((item1.Price * quantity) + (item2?.Price ?? 0)) * 0.1m,
                VatRate = 0.1m,
                CreatedByEmployee = admin,
                CreatedAt = DateTime.SpecifyKind(date.AddHours(12), DateTimeKind.Utc),
                PaidAt = DateTime.SpecifyKind(date.AddHours(13), DateTimeKind.Utc),
            };

            order.OrderItems.Add(
                new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    MenuItemId = item1.MenuItemId,
                    ItemCodeSnapshot = item1.Code,
                    ItemNameSnapshot = item1.Name,
                    StationSnapshot = item1.Station.ToString(),
                    Status = OrderItemStatus.Completed,
                    Quantity = quantity,
                    UnitPriceSnapshot = item1.Price,
                    CreatedAt = order.CreatedAt,
                }
            );

            if (item2 != null)
            {
                order.OrderItems.Add(
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        MenuItemId = item2.MenuItemId,
                        ItemCodeSnapshot = item2.Code,
                        ItemNameSnapshot = item2.Name,
                        StationSnapshot = item2.Station.ToString(),
                        Status = OrderItemStatus.Completed,
                        Quantity = 1,
                        UnitPriceSnapshot = item2.Price,
                        CreatedAt = order.CreatedAt,
                    }
                );
            }

            return order;
        }

        private void SyncOccupiedTablesFromActiveOrders()
        {
            var occupiedTableIds = _context
                .Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Serving && o.TableId.HasValue)
                .Select(o => o.TableId!.Value)
                .Distinct()
                .ToList();

            if (occupiedTableIds.Count == 0)
            {
                return;
            }

            var tablesToUpdate = _context
                .Tables.Where(t =>
                    occupiedTableIds.Contains(t.TableId) && t.Status != TableStatus.Occupied
                )
                .ToList();

            if (tablesToUpdate.Count == 0)
            {
                return;
            }

            foreach (var table in tablesToUpdate)
            {
                table.Status = TableStatus.Occupied;
                table.UpdatedAt = DateTime.UtcNow;
            }

            _context.SaveChanges();
        }

        private void RepairKnownSchemaDrift()
        {
            _context.Database.ExecuteSqlRaw(
                """
                ALTER TABLE order_items
                ADD COLUMN IF NOT EXISTS combo_parent_order_item_id uuid NULL;
                """
            );
        }


    }
}
