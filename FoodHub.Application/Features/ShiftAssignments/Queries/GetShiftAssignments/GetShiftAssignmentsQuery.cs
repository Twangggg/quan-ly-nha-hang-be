using FoodHub.Application.Common.Models;
using MediatR;
 
namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignments
{
    public record GetShiftAssignmentsQuery(PaginationParams? Pagination = null) : IRequest<Result<PagedResult<GetShiftAssignmentsResponse>>>
    {
        public PaginationParams Pagination { get; } = Pagination ?? new();
    }
}
