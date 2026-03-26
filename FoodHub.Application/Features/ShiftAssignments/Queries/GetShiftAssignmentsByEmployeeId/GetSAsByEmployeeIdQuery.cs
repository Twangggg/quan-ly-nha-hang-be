using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftsByEmployeeId
{
    public record GetSAsByEmployeeIdQuery(
        PaginationParams Pagination
        ) : IRequest<Result<PagedResult<GetSAsByEmployeeIdResponse>>>;
}
