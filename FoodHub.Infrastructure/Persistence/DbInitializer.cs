using FoodHub.Application.Interfaces;
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
            // Auto Migrate
            if (_context.Database.GetPendingMigrations().Any())
            {
                _context.Database.Migrate();
            }

            // Seed Data
            if (!_context.Employees.Any())
            {
                var employees = new Employee[]
                {
                    new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        EmployeeCode = "M001001",
                        Username = "admin",
                        PasswordHash = _passwordService.HashPassword("New123!"),
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
                        EmployeeCode = "B002001",
                        Username = "chef",
                        PasswordHash = _passwordService.HashPassword("New123!"),
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
                        EmployeeCode = "W003001",
                        Username = "waiter",
                        PasswordHash = _passwordService.HashPassword("New123!"),
                        FullName = "Waiter One",
                        Email = "waiter@foodhub.com",
                        Phone = "0909000003",
                        Role = EmployeeRole.Waiter,
                        Status = EmployeeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        EmployeeCode = "C004001",
                        Username = "cashier",
                        PasswordHash = _passwordService.HashPassword("New123!"),
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

                        // Add Audit Log for Seed Data
                        _context.AuditLogs.Add(
                            new AuditLog
                            {
                                LogId = Guid.NewGuid(),
                                Action = AuditAction.Create,
                                TargetId = e.EmployeeId,
                                PerformedByEmployeeId = e.EmployeeId, // Self-created for seed
                                CreatedAt = DateTimeOffset.UtcNow,
                                Reason = "Seed data initialization",
                                Metadata = "{\"info\": \"System generated\"}", // Valid JSON for jsonb column
                            }
                        );
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
                        Code = "DRK-007",
                        ItemNumber = 7,
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
                        Code = "DES-003",
                        ItemNumber = 3,
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
                };

                _context.MenuItems.AddRange(menuItems);
                _context.SaveChanges();
            }

            if (!_context.SetMenus.Any())
            {
                var comboCategory = _context.Categories.First(c => c.Name == "Combo");

                var setMenu1 = new SetMenu
                {
                    SetMenuId = Guid.NewGuid(),
                    Code = "COMBO-01",
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
                    Code = "COMBO-02",
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
                var chickenRice = _context.MenuItems.First(mi => mi.Code == "MAIN-001");
                var specialDrink = _context.MenuItems.First(mi => mi.Code == "DRK-007");

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

            // Seed Ingredients for Inventory module
            if (!_context.Ingredients.Any())
            {
                var ingredients = new List<Ingredient>
                {
                    Ingredient.Create("ING-001", "Thịt bò", "kg", 5, "Thịt bò tươi cho món chính"),
                    Ingredient.Create("ING-002", "Ức gà", "kg", 8, "Ức gà fillet không da"),
                    Ingredient.Create("ING-003", "Rau xà lách", "kg", 3, "Rau xà lách Đà Lạt"),
                    Ingredient.Create("ING-004", "Khoai tây", "kg", 10, "Khoai tây Hà Lan"),
                    Ingredient.Create("ING-005", "Hành tây", "kg", 6, "Hành tây tím"),
                    Ingredient.Create("ING-006", "Sữa tươi", "l", 12, "Sữa tươi tiệt trùng"),
                };

                // Cập nhật tồn kho và giá vốn ban đầu
                var seedStock = new (string Code, decimal Quantity, decimal Cost)[]
                {
                    ("ING-001", 25, 180000),
                    ("ING-002", 30, 120000),
                    ("ING-003", 12, 45000),
                    ("ING-004", 40, 35000),
                    ("ING-005", 18, 28000),
                    ("ING-006", 24, 32000),
                };

                foreach (var ingredient in ingredients)
                {
                    var stockInfo = seedStock.First(s => s.Code == ingredient.Code);
                    ingredient.UpdateStock(stockInfo.Quantity, stockInfo.Cost);
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
                        Name = "VIP Room",
                        CodePrefix = "VIP",
                        Type = AreaType.VIP,
                        Description = "Private VIP rooms",
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
                    var areaId =
                        i <= 8 ? Guid.Parse("00000000-0000-0000-0000-000000000001")
                        : i <= 10 ? Guid.Parse("00000000-0000-0000-0000-000000000002")
                        : Guid.Parse("00000000-0000-0000-0000-000000000003");

                    _context.Tables.Add(
                        new Table
                        {
                            TableId = tableId,
                            TableNumber = i,
                            Capacity = i <= 8 ? (i % 2 == 0 ? 4 : 2) : (i <= 10 ? 4 : 8),
                            AreaId = areaId,
                            Status = TableStatus.Available,
                            CreatedAt = DateTime.UtcNow,
                        }
                    );
                }
            }
            _context.SaveChanges();

            if (!_context.Orders.Any())
            {
                var admin = _context.Employees.First(e => e.EmployeeCode == "M001001");
                var chickenRice = _context.MenuItems.First(mi => mi.Code == "MAIN-001");
                var beefNoodle = _context.MenuItems.First(mi => mi.Code == "MAIN-002");
                var specialDrink = _context.MenuItems.First(mi => mi.Code == "DRK-007");

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
                        Status = OrderItemStatus.Ready,
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
                        Status = OrderItemStatus.Ready,
                        Quantity = 1,
                        UnitPriceSnapshot = specialDrink.Price,
                        CreatedAt = order3.CreatedAt,
                    }
                );

                _context.Orders.AddRange(order1, order2, order3);
                _context.SaveChanges();
            }

            _context.SaveChanges();
        }
    }
}
