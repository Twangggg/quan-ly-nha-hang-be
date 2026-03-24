using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using MediatR;
 
namespace FoodHub.Application.Features.Shifts.Queries.GetShifts
{
    public record GetShiftsQuery(PaginationParams? Pagination = null) : IRequest<Result<PagedResult<GetShiftByIdResponse>>>
    {
        public PaginationParams Pagination { get; } = Pagination ?? new();
    }
}
