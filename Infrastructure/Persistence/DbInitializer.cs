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
            if (_context.Employees.Any())
            {
                return;
            }

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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
                }
            };

            foreach (var e in employees)
            {
                // Check if already exists to avoid duplicate key errors 
                if (!_context.Employees.Any(x => x.EmployeeCode == e.EmployeeCode || x.Username == e.Username || x.Email == e.Email))
                {
                    _context.Employees.Add(e);

                    // Add Audit Log for Seed Data
                    _context.AuditLogs.Add(new AuditLog
                    {
                        LogId = Guid.NewGuid(),
                        Action = AuditAction.Create,
                        TargetId = e.EmployeeId,
                        PerformedByEmployeeId = e.EmployeeId, // Self-created for seed
                        CreatedAt = DateTimeOffset.UtcNow,
                        Reason = "Seed data initialization",
                        Metadata = "{\"info\": \"System generated\"}" // Valid JSON for jsonb column
                    });
                }
            }
            _context.SaveChanges();

            // Seed Categories
            if (!_context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category
                    {
                        CategoryId = Guid.Parse("b8f0fcc2-9f8a-4baf-8a3c-1d0e8f6b7a2c"),
                        Name = "Món Cơm",
                        Type = CategoryType.Food,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        CategoryId = Guid.Parse("c2a3b4c5-d6e7-f8a9-0b1c-2d3e4f5a6b7c"),
                        Name = "Món Nước",
                        Type = CategoryType.Drink,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        CategoryId = Guid.Parse("d3b4c5d6-e7f8-a9b0-c1d2-e3f4a5b6c7d8"),
                        Name = "Món Phở",
                        Type = CategoryType.Food,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.Categories.AddRange(categories);
                _context.SaveChanges();
            }

            // Seed MenuItems
            if (!_context.MenuItems.Any())
            {
                var menuItems = new MenuItem[]
                {
                    new MenuItem
                    {
                        MenuItemId = Guid.Parse("a1111111-1111-4111-8111-111111111111"),
                        Code = "MI001",
                        Name = "Cơm gà xối mỡ",
                        ImageUrl = "https://picsum.photos/seed/mi001/600/",
                        Description = "Gà phi, cơm nóng, ức gà luộc mềm qua...",
                        CategoryId = Guid.Parse("b8f0fcc2-9f8a-4baf-8a3c-1d0e8f6b7a2c"),
                        Station = Station.HotKitchen,
                        ExpectedTime = 15,
                        PriceDineIn = 45000,
                        PriceTakeAway = 40000,
                        Cost = 20000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.Parse("a2222222-2222-4222-8222-222222222222"),
                        Code = "MI002",
                        Name = "Cơm tấm sườn",
                        ImageUrl = "https://picsum.photos/seed/mi002/600/",
                        Description = "Sườn nướng, bì, chả, trứng",
                        CategoryId = Guid.Parse("b8f0fcc2-9f8a-4baf-8a3c-1d0e8f6b7a2c"),
                        Station = Station.HotKitchen,
                        ExpectedTime = 20,
                        PriceDineIn = 50000,
                        PriceTakeAway = 45000,
                        Cost = 25000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.Parse("a3333333-3333-4333-8333-333333333333"),
                        Code = "MI003",
                        Name = "Bún bò Huế",
                        ImageUrl = "https://picsum.photos/seed/mi003/600/",
                        Description = "Nước dùng hầm xương, bò viên, bò lá lốt",
                        CategoryId = Guid.Parse("d3b4c5d6-e7f8-a9b0-c1d2-e3f4a5b6c7d8"),
                        Station = Station.HotKitchen,
                        ExpectedTime = 18,
                        PriceDineIn = 48000,
                        PriceTakeAway = 43000,
                        Cost = 22000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.Parse("a4444444-4444-4444-8444-444444444444"),
                        Code = "MI004",
                        Name = "Phở bò tái",
                        ImageUrl = "https://picsum.photos/seed/mi004/600/",
                        Description = "Phở bò tái, nước dùng trong, hành tây",
                        CategoryId = Guid.Parse("d3b4c5d6-e7f8-a9b0-c1d2-e3f4a5b6c7d8"),
                        Station = Station.HotKitchen,
                        ExpectedTime = 16,
                        PriceDineIn = 52000,
                        PriceTakeAway = 47000,
                        Cost = 26000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new MenuItem
                    {
                        MenuItemId = Guid.Parse("a5555555-5555-4555-8555-555555555555"),
                        Code = "MI005",
                        Name = "Nước suối 500ml",
                        ImageUrl = "https://picsum.photos/seed/mi005/600/",
                        Description = "Nước khoáng tinh khiết",
                        CategoryId = Guid.Parse("c2a3b4c5-d6e7-f8a9-0b1c-2d3e4f5a6b7c"),
                        Station = Station.Bar,
                        ExpectedTime = 5,
                        PriceDineIn = 10000,
                        PriceTakeAway = 10000,
                        Cost = 5000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.MenuItems.AddRange(menuItems);
                _context.SaveChanges();
            }

            // Seed OptionGroups 
            if (!_context.OptionGroups.Any())
            {
                var optionGroups = new OptionGroup[]
                {
                    new OptionGroup
                    {
                        OptionGroupId = Guid.Parse("10111111-1111-4111-8111-111111111111"),
                        MenuItemId = Guid.Parse("a1111111-1111-4111-8111-111111111111"), // Cơm gà
                        Name = "Độ cay",
                        Type = OptionGroupType.Single,
                        IsRequired = false
                    },
                    new OptionGroup
                    {
                        OptionGroupId = Guid.Parse("20222222-2222-4222-8222-222222222222"),
                        MenuItemId = Guid.Parse("a3333333-3333-4333-8333-333333333333"), // Bún bò Huế
                        Name = "Topping thêm",
                        Type = OptionGroupType.Multi,
                        IsRequired = false
                    }
                };

                _context.OptionGroups.AddRange(optionGroups);
                _context.SaveChanges();
            }

            // Seed OptionItems 
            if (!_context.OptionItems.Any())
            {
                var optionItems = new OptionItem[]
                {
                    // Độ cay cho Cơm gà
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("01111111-1111-4111-8111-111111111111"),
                        OptionGroupId = Guid.Parse("10111111-1111-4111-8111-111111111111"),
                        Label = "Không cay",
                        ExtraPrice = 0
                    },
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("02222222-2222-4222-8222-222222222222"),
                        OptionGroupId = Guid.Parse("10111111-1111-4111-8111-111111111111"),
                        Label = "Ít cay",
                        ExtraPrice = 0
                    },
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("03333333-3333-4333-8333-333333333333"),
                        OptionGroupId = Guid.Parse("10111111-1111-4111-8111-111111111111"),
                        Label = "Cay vừa",
                        ExtraPrice = 0
                    },
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("04444444-4444-4444-8444-444444444444"),
                        OptionGroupId = Guid.Parse("10111111-1111-4111-8111-111111111111"),
                        Label = "Cay nhiều",
                        ExtraPrice = 0
                    },
                    // Topping thêm cho Bún bò Huế
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("05555555-5555-4555-8555-555555555555"),
                        OptionGroupId = Guid.Parse("20222222-2222-4222-8222-222222222222"),
                        Label = "Thêm bò viên",
                        ExtraPrice = 10000
                    },
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("06666666-6666-4666-8666-666666666666"),
                        OptionGroupId = Guid.Parse("20222222-2222-4222-8222-222222222222"),
                        Label = "Thêm chả cua",
                        ExtraPrice = 15000
                    },
                    new OptionItem
                    {
                        OptionItemId = Guid.Parse("07777777-7777-4777-8777-777777777777"),
                        OptionGroupId = Guid.Parse("20222222-2222-4222-8222-222222222222"),
                        Label = "Thêm giò heo",
                        ExtraPrice = 12000
                    }
                };

                _context.OptionItems.AddRange(optionItems);
                _context.SaveChanges();
            }

            // Seed SetMenus
            if (!_context.SetMenus.Any())
            {
                var setMenus = new SetMenu[]
                {
                    new SetMenu
                    {
                        SetMenuId = Guid.Parse("sm111111-1111-4111-8111-111111111111"),
                        Code = "SET001",
                        Name = "Combo Cơm Gà + Nước",
                        Price = 50000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SetMenu
                    {
                        SetMenuId = Guid.Parse("sm222222-2222-4222-8222-222222222222"),
                        Code = "SET002",
                        Name = "Combo Phở + Nước",
                        Price = 58000,
                        IsOutOfStock = false,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.SetMenus.AddRange(setMenus);
                _context.SaveChanges();
            }

            // Seed SetMenuItems (NO CreatedAt/UpdatedAt for nhudm entities)
            if (!_context.SetMenuItems.Any())
            {
                var setMenuItems = new SetMenuItem[]
                {
                    // Combo Cơm Gà + Nước
                    new SetMenuItem
                    {
                        SetMenuItemId = Guid.Parse("smi11111-1111-4111-8111-111111111111"),
                        SetMenuId = Guid.Parse("sm111111-1111-4111-8111-111111111111"),
                        MenuItemId = Guid.Parse("a1111111-1111-4111-8111-111111111111"), // Cơm gà
                        Quantity = 1
                    },
                    new SetMenuItem
                    {
                        SetMenuItemId = Guid.Parse("smi22222-2222-4222-8222-222222222222"),
                        SetMenuId = Guid.Parse("sm111111-1111-4111-8111-111111111111"),
                        MenuItemId = Guid.Parse("a5555555-5555-4555-8555-555555555555"), // Nước suối
                        Quantity = 1
                    },
                    // Combo Phở + Nước
                    new SetMenuItem
                    {
                        SetMenuItemId = Guid.Parse("smi33333-3333-4333-8333-333333333333"),
                        SetMenuId = Guid.Parse("sm222222-2222-4222-8222-222222222222"),
                        MenuItemId = Guid.Parse("a4444444-4444-4444-8444-444444444444"), // Phở bò
                        Quantity = 1
                    },
                    new SetMenuItem
                    {
                        SetMenuItemId = Guid.Parse("smi44444-4444-4444-8444-444444444444"),
                        SetMenuId = Guid.Parse("sm222222-2222-4222-8222-222222222222"),
                        MenuItemId = Guid.Parse("a5555555-5555-4555-8555-555555555555"), // Nước suối
                        Quantity = 1
                    }
                };

                _context.SetMenuItems.AddRange(setMenuItems);
                _context.SaveChanges();
            }
        }
    }
}
