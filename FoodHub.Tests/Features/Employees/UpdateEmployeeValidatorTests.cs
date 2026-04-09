using FluentAssertions;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Employees.Commands.UpdateEmployee;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using Moq;

namespace FoodHub.Tests.Features.Employees
{
    public class UpdateEmployeeValidatorTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IMessageService> _messageService = new();
        private readonly Mock<IGenericRepository<Employee>> _employeeRepository = new();

        public UpdateEmployeeValidatorTests()
        {
            _messageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns<string>(key => key);

            _unitOfWork
                .Setup(x => x.Repository<Employee>())
                .Returns(_employeeRepository.Object);

            _employeeRepository
                .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>()))
                .ReturnsAsync(false);
        }

        [Fact]
        public async Task ValidateAsync_Should_Fail_When_DateOfBirth_Is_In_The_Future()
        {
            var validator = new UpdateEmployeeValidator(_unitOfWork.Object, _messageService.Object);
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd");
            var command = new UpdateEmployeeCommand(
                Guid.NewGuid(),
                null,
                "Nguyen Van A",
                null,
                null,
                null,
                tomorrow
            );

            var result = await validator.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(x =>
                x.ErrorMessage == MessageKeys.Profile.DateOfBirthMustBePast
            );
        }
    }
}
