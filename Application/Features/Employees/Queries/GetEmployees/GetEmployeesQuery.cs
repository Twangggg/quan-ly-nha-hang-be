using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployees
{
    public record GetEmployeesQuery(PaginationParams Pagination) : IRequest<PagedResult<GetEmployeesResponse>>;
}
