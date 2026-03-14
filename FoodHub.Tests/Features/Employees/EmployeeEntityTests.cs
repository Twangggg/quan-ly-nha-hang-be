using FluentAssertions;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Tests.Features.Employees
{
    public class EmployeeEntityTests
    {
        [Fact]
        public void UpdateDetails_Should_NormalizeOptionalFields_And_UpdateAuditInfo()
        {
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                FullName = "Old Name",
                Email = "john@example.com",
                Status = EmployeeStatus.Active,
            };
            var auditorId = Guid.NewGuid();

            employee.UpdateDetails(
                "New Name",
                "",
                "   ",
                null,
                new DateOnly(1990, 1, 1),
                EmployeeStatus.Inactive,
                auditorId
            );

            employee.FullName.Should().Be("New Name");
            employee.Username.Should().BeNull();
            employee.Phone.Should().BeNull();
            employee.Address.Should().BeNull();
            employee.DateOfBirth.Should().Be(new DateOnly(1990, 1, 1));
            employee.Status.Should().Be(EmployeeStatus.Inactive);
            employee.UpdatedBy.Should().Be(auditorId);
            employee.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void DeleteEmployee_Should_Throw_When_EmployeeAlreadyInactive()
        {
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                FullName = "John Doe",
                Email = "john@example.com",
                Status = EmployeeStatus.Inactive,
            };

            var action = () => employee.DeleteEmployee(Guid.NewGuid());

            action.Should().Throw<InvalidOperationException>();
        }
    }
}
