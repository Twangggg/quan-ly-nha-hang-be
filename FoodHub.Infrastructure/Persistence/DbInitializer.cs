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
                        Email = "admin@foodhub.com",
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
                var foodCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Main Dishes",
                    CategoryType = CategoryType.Normal,
                    CreatedAt = DateTime.UtcNow,
                };

                var drinkCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Drinks",
                    CategoryType = CategoryType.Normal,
                    CreatedAt = DateTime.UtcNow,
                };

                var comboCategory = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = "Combo Sets",
                    CategoryType = CategoryType.SpecialGroup,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Categories.AddRange(foodCategory, drinkCategory, comboCategory);
                _context.SaveChanges();

                var menuItems = new List<MenuItem>
                {
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "FOOD001",
                        Name = "Grilled Chicken Rice",
                        ImageUrl = "images/chicken_rice.jpg",
                        Description = "Grilled chicken served with fragrant rice",
                        CategoryId = foodCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 15,
                        PriceDineIn = 50000,
                        PriceTakeAway = 48000,
                        CostPrice = 30000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "FOOD002",
                        Name = "Beef Noodle Soup",
                        ImageUrl = "images/beef_noodle.jpg",
                        Description = "Traditional beef noodle soup",
                        CategoryId = foodCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 12,
                        PriceDineIn = 45000,
                        PriceTakeAway = 43000,
                        CostPrice = 25000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "DRINK001",
                        Name = "Lemon Tea",
                        ImageUrl = "images/lemon_tea.jpg",
                        Description = "Refreshing iced lemon tea",
                        CategoryId = drinkCategory.CategoryId,
                        Station = Station.Bar,
                        ExpectedTime = 3,
                        PriceDineIn = 20000,
                        PriceTakeAway = 18000,
                        CostPrice = 8000,
                        CreatedAt = DateTime.UtcNow,
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.NewGuid(),
                        Code = "COMBO001",
                        Name = "Chicken Combo Set",
                        ImageUrl = "images/chicken_combo.jpg",
                        Description = "Chicken rice + drink combo",
                        CategoryId = comboCategory.CategoryId,
                        Station = Station.HotKitchen,
                        ExpectedTime = 18,
                        PriceDineIn = 65000,
                        PriceTakeAway = 62000,
                        CostPrice = 40000,
                        CreatedAt = DateTime.UtcNow,
                    },
                };

                _context.MenuItems.AddRange(menuItems);
                _context.SaveChanges();
            }

            if (!_context.SetMenus.Any())
            {
                var setMenu = new SetMenu
                {
                    SetMenuId = Guid.NewGuid(),
                    Code = "SET001",
                    Name = "Lunch Set",
                    Price = 70000,
                    IsOutOfStock = false,
                    CreatedAt = DateTime.UtcNow,
                };

                var setMenu2 = new SetMenu
                {
                    SetMenuId = Guid.NewGuid(),
                    Code = "SET002",
                    Name = "Dinner Set",
                    Price = 85000,
                    IsOutOfStock = false,
                    CreatedAt = DateTime.UtcNow,
                };

                var setMenuItem21 = new SetMenuItem
                {
                    SetMenuItemId = Guid.NewGuid(),
                    SetMenuId = setMenu2.SetMenuId,
                    MenuItemId = _context.MenuItems.First(mi => mi.Code == "FOOD002").MenuItemId,
                    Quantity = 1,
                };

                var setMenuItem22 = new SetMenuItem
                {
                    SetMenuItemId = Guid.NewGuid(),
                    SetMenuId = setMenu2.SetMenuId,
                    MenuItemId = _context.MenuItems.First(mi => mi.Code == "DRINK001").MenuItemId,
                    Quantity = 1,
                };

                var setMenu3 = new SetMenu
                {
                    SetMenuId = Guid.NewGuid(),
                    Code = "SET003",
                    Name = "Family Set",
                    Price = 150000,
                    IsOutOfStock = false,
                    CreatedAt = DateTime.UtcNow,
                };

                var setMenuItem31 = new SetMenuItem
                {
                    SetMenuItemId = Guid.NewGuid(),
                    SetMenuId = setMenu3.SetMenuId,
                    MenuItemId = _context.MenuItems.First(mi => mi.Code == "FOOD001").MenuItemId,
                    Quantity = 2,
                };

                _context.SetMenus.AddRange(setMenu, setMenu2, setMenu3);
                _context.SaveChanges();
            }

            if (!_context.Orders.Any())
            {
                var admin = _context.Employees.First(e => e.EmployeeCode == "M001001");
                var chickenRice = _context.MenuItems.First(mi => mi.Code == "FOOD001");
                var beefNoodle = _context.MenuItems.First(mi => mi.Code == "FOOD002");
                var lemonTea = _context.MenuItems.First(mi => mi.Code == "DRINK001");

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
                    TotalAmount = 70000,
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
                        UnitPriceSnapshot = chickenRice.PriceDineIn,
                        CreatedAt = order1.CreatedAt,
                    }
                );

                order1.OrderItems.Add(
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order1.OrderId,
                        MenuItemId = lemonTea.MenuItemId,
                        ItemCodeSnapshot = lemonTea.Code,
                        ItemNameSnapshot = lemonTea.Name,
                        StationSnapshot = lemonTea.Station.ToString(),
                        Status = OrderItemStatus.Completed,
                        Quantity = 1,
                        UnitPriceSnapshot = lemonTea.PriceDineIn,
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
                    TotalAmount = 45000,
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
                        UnitPriceSnapshot = beefNoodle.PriceDineIn,
                        CreatedAt = order2.CreatedAt,
                    }
                );

                var order3 = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = $"ORD-{DateTime.Now:yyyyMMdd}-0003",
                    OrderType = OrderType.Takeaway,
                    Status = OrderStatus.Serving,
                    TotalAmount = 20000,
                    CreatedByEmployee = admin,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                };

                order3.OrderItems.Add(
                    new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order3.OrderId,
                        MenuItemId = lemonTea.MenuItemId,
                        ItemCodeSnapshot = lemonTea.Code,
                        ItemNameSnapshot = lemonTea.Name,
                        StationSnapshot = lemonTea.Station.ToString(),
                        Status = OrderItemStatus.Ready,
                        Quantity = 1,
                        UnitPriceSnapshot = lemonTea.PriceDineIn,
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
