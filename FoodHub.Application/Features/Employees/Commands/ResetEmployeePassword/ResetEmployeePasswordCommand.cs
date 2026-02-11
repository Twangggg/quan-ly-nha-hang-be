using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword
{
    public record ResetEmployeePasswordCommand
    (
        Guid EmployeeId,
        string Reason,
        string? NewPassword = null
        ) : IRequest<Result<ResetEmployeePasswordResponse>>;
}
