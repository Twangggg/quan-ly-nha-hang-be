using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Infrastructure.Persistence
{
    public class DbInitializer
    {
        private readonly AppDbContext _context;

        public DbInitializer(AppDbContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            // Kiểm tra xem đã có dữ liệu chưa
            if (_context.Employees.Any())
            {
                return;   // DB has been seeded
            }

            var employees = new Employee[]
            {
                new Employee
                {
                    EmployeeId = Guid.NewGuid(),
                    EmployeeCode = "NV001",
                    Username = "admin",
                    PasswordHash = "admin", // Placeholder: Nên thay bằng mã hóa thực tế khi có Auth
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
                    PasswordHash = "chef",
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
                    PasswordHash = "waiter",
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
                    PasswordHash = "cashier",
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
