using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployees
{
    public record GetEmployeesQuery(PaginationParams Pagination) : IRequest<Result<PagedResult<GetEmployeesResponse>>>;
}
