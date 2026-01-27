using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            if (!await context.Employees.AnyAsync())
            {
                var admin = new Employee
                {
                    EmployeeId = Guid.NewGuid(),
                    EmployeeCode = "NV000001",
                    Username = "admin",
                    PasswordHash = passwordHasher.HashPassword("Password123!"),
                    FullName = "Admin User",
                    Email = "admin@foodhub.com",
                    Phone = "0000000000",
                    Role = EmployeeRole.Manager,
                    Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Employees.AddAsync(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}
