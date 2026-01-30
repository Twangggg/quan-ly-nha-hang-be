using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence
{
    public class DbInitializer
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public DbInitializer(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
                    EmployeeCode = "NV001",
                    Username = "admin",
                    PasswordHash = _passwordHasher.HashPassword("admin"), // Hash password
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
                    EmployeeCode = "NV002",
                    Username = "chef",
                    PasswordHash = _passwordHasher.HashPassword("chef"),
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
                    EmployeeCode = "NV003",
                    Username = "waiter",
                    PasswordHash = _passwordHasher.HashPassword("waiter"),
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
                    EmployeeCode = "NV004",
                    Username = "cashier",
                    PasswordHash = _passwordHasher.HashPassword("cashier"),
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
                _context.Employees.Add(e);
            }
            _context.SaveChanges();
        }
    }
}
