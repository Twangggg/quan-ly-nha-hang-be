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
                    EmployeeCode = "NV001",
                    Username = "admin",
                    PasswordHash = _passwordService.HashPassword("admin"), // Hash password
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
                    PasswordHash = _passwordService.HashPassword("chef"),
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
                    PasswordHash = _passwordService.HashPassword("waiter"),
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
                    PasswordHash = _passwordService.HashPassword("cashier"),
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

                // Add Audit Log for Seed Data
                _context.AuditLogs.Add(new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    Action = AuditAction.Create,
                    TargetId = e.EmployeeId,
                    PerformedByEmployeeId = e.EmployeeId, // Self-created for seed
                    CreatedAt = DateTimeOffset.UtcNow,
                    Reason = "Seed data initialization",
                    Metadata = "System generated"
                });
            }
            _context.SaveChanges();
        }
    }
}
