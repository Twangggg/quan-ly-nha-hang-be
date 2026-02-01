using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public record Command(
        Guid EmployeeId,
        string FullName,
        string Email,
        string Phone,
        string? Address,
        DateOnly? DateOfBirth
        ) : IRequest<Result<Response>>;
}
