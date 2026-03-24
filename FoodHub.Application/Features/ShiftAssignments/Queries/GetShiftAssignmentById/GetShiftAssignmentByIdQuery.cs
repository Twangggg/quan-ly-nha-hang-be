using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignmentById
{
    /// <summary>
    /// Query để lấy chi tiết một phân công ca theo ID.
    /// </summary>
    /// <param name="ShiftAssignmentId">ID của bản ghi phân công.</param>
    public record GetShiftAssignmentByIdQuery(Guid ShiftAssignmentId)
        : IRequest<Result<GetShiftAssignmentByIdResponse>>;
}
