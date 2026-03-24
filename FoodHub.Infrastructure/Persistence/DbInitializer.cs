using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
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
                var comboCategory = _context.Categories.FirstOrDefault(c => c.Name == "Combo");

                if (comboCategory != null)
                {
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
                    var chickenRice = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-001");
                    var specialDrink = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-007");

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
                }
            }

            // Seed Ingredients for Inventory module
            if (!_context.Ingredients.Any())
            {
                var seedIngredients = new[]
                {
                    new
                    {
                        Code = "THITBO",
                        Name = "Thịt bò",
                        Unit = "kg",
                        LowStockThreshold = 10m,
                        Description = "Thịt bò tươi cho món chính",
                        Stock = 0m,
                        Cost = 0m,
                    },
                    new
                    {
                        Code = "UCGA",
                        Name = "Ức gà",
                        Unit = "kg",
                        LowStockThreshold = 12m,
                        Description = "Ức gà fillet không da",
                        Stock = 0m,
                        Cost = 0m,
                    },
                    new
                    {
                        Code = "RAUXALACH",
                        Name = "Rau xà lách",
                        Unit = "kg",
                        LowStockThreshold = 5m,
                        Description = "Rau xà lách Đà Lạt",
                        Stock = 0m,
                        Cost = 0m,
                    },
                    new
                    {
                        Code = "KHOAITAY",
                        Name = "Khoai tây",
                        Unit = "kg",
                        LowStockThreshold = 15m,
                        Description = "Khoai tây Hà Lan",
                        Stock = 0m,
                        Cost = 0m,
                    },
                    new
                    {
                        Code = "HANHTAY",
                        Name = "Hành tây",
                        Unit = "kg",
                        LowStockThreshold = 8m,
                        Description = "Hành tây tím",
                        Stock = 0m,
                        Cost = 0m,
                    },
                    new
                    {
                        Code = "SUATUOI",
                        Name = "Sữa tươi",
                        Unit = "l",
                        LowStockThreshold = 20m,
                        Description = "Sữa tươi tiệt trùng",
                        Stock = 0m,
                        Cost = 0m,
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
                        seed.Description
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

            if (!_context.Orders.Any())
            {
                var admin = _context.Employees.FirstOrDefault(e => e.EmployeeCode == "M001001");
                var chickenRice = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-001");
                var beefNoodle = _context.MenuItems.FirstOrDefault(mi => mi.Code == "MAIN-002");
                var specialDrink = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-007");

                if (admin == null || chickenRice == null || beefNoodle == null || specialDrink == null)
                {
                    // Basic dependencies are missing, skip seeding orders as it depends on these specific items
                    return;
                }

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

                // Update Table statuses for seeded orders
                var table1 = _context.Tables.Local.FirstOrDefault(t => t.TableId == table01Id)
                             ?? _context.Tables.FirstOrDefault(t => t.TableId == table01Id);
                var table2 = _context.Tables.Local.FirstOrDefault(t => t.TableId == table02Id)
                             ?? _context.Tables.FirstOrDefault(t => t.TableId == table02Id);

                if (table1 != null) table1.Status = TableStatus.Occupied;
                if (table2 != null) table2.Status = TableStatus.Occupied;

                _context.SaveChanges();
            }

            SyncOccupiedTablesFromActiveOrders();

            // Seed an invoice for the first order to demonstrate the relationship and for FE testing
            var environmentName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevOrDemo = string.Equals(environmentName, "Development", System.StringComparison.OrdinalIgnoreCase)
                              || string.Equals(environmentName, "Demo", System.StringComparison.OrdinalIgnoreCase);

            if (isDevOrDemo && !_context.Invoices.Any())
            {
                // Use the first available order (if any) without relying on hard-coded order codes/IDs
                var order1 = _context.Orders.Include(o => o.OrderItems).FirstOrDefault();
                if (order1 != null)
                {
                    var invoice1 = new Invoice
                    {
                        InvoiceId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        OrderId = order1.OrderId,
                        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-0001",
                        SubTotal = order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot),
                        TaxAmount = order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot) * 0.1m, // Assuming 10% tax
                        DiscountAmount = 0m,
                        TotalAmount = order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot) * 1.1m, // Subtotal + Tax
                        AmountReceived = order1.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceSnapshot) * 1.1m,
                        AmountReturned = 0m,
                        PaymentMethod = PaymentMethod.Cash,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Invoices.Add(invoice1);
                    _context.SaveChanges();

                    if (!_context.InvoiceItems.Any(ii => ii.InvoiceId == invoice1.InvoiceId))
                    {
                        var invoiceItems = order1.OrderItems.Select(oi => new InvoiceItem
                        {
                            InvoiceId = invoice1.InvoiceId,
                            ItemName = oi.ItemNameSnapshot,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPriceSnapshot,
                            TotalPrice = oi.Quantity * oi.UnitPriceSnapshot,
                            Note = oi.ItemNote,
                            CreatedAt = DateTime.UtcNow
                        }).ToList();
                        _context.InvoiceItems.AddRange(invoiceItems);
                    }

                    _context.SaveChanges();
                }
            }

            if (isDevOrDemo && !_context.Vouchers.Any())
            {
                var voucher1 = new Voucher
                {
                    VoucherId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    VoucherCode = "DISCOUNT10",
                    VoucherType = VoucherType.Percent,
                    DiscountValue = 10m,
                    MaxDiscount = 50000m,
                    MinOrderValue = 100000m,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var drinkMenuItem = _context.MenuItems.FirstOrDefault(mi => mi.Code == "DRK-007");

                if (drinkMenuItem != null)
                {
                    var voucher2 = new Voucher
                    {
                        VoucherId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        VoucherCode = "FREEDRINK",
                        VoucherType = VoucherType.FreeItem,
                        ItemId = drinkMenuItem.MenuItemId, // Tặng cocktail đặc biệt
                        FreeQuantity = 1,
                        MinOrderValue = 200000m,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.Vouchers.Add(voucher2);
                }

                _context.Vouchers.Add(voucher1);
                _context.SaveChanges();
            }

            if (isDevOrDemo && !_context.Shifts.Any())
            {
                var shiftMorning = new Shift
                {
                    ShiftId = Guid.NewGuid(),
                    Name = "Ca sáng",
                    StartTime = new TimeSpan(10, 30, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                };

                var shiftAfternoon = new Shift
                {
                    ShiftId = Guid.NewGuid(),
                    Name = "Ca chiều",
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(23, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Shifts.AddRange(shiftMorning, shiftAfternoon);
                _context.SaveChanges();

                if (isDevOrDemo && !_context.ShiftAssignments.Any())
                {
                    var cashierShift1 = new ShiftAssignment
                    {
                        ShiftAssignmentId = Guid.NewGuid(),
                        ShiftId = shiftMorning.ShiftId,
                        EmployeeId = _context.Employees.First(e => e.EmployeeCode == "C004001").EmployeeId,
                        CreatedAt = DateTime.UtcNow,
                    };

                    var waiterShift1 = new ShiftAssignment
                    {
                        ShiftAssignmentId = Guid.NewGuid(),
                        ShiftId = shiftAfternoon.ShiftId,
                        EmployeeId = _context.Employees.First(e => e.EmployeeCode == "W003001").EmployeeId,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.ShiftAssignments.AddRange(cashierShift1, waiterShift1);
                    _context.SaveChanges();
                }
            }

            _context.SaveChanges();
        }

        private void SyncOccupiedTablesFromActiveOrders()
        {
            var occupiedTableIds = _context
                .Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Serving && o.TableId.HasValue)
                .Select(o => o.TableId!.Value)
                .Distinct()
                .ToList();

            if (occupiedTableIds.Count == 0)
            {
                return;
            }

            var tablesToUpdate = _context
                .Tables
                .Where(t => occupiedTableIds.Contains(t.TableId) && t.Status != TableStatus.Occupied)
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
    }
}
