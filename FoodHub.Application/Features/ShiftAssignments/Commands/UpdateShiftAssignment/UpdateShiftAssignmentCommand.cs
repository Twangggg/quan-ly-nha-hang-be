using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.UpdateShiftAssignment
{
    public record UpdateShiftAssignmentCommand : IRequest<Result<AssignShiftResponse>>
    {
        public Guid ShiftAssignmentId { get; init; }
        public required Guid ShiftId { get; init; }
        public required DateOnly AssignedDate { get; init; }
        public string? Note { get; init; }
    }
}
