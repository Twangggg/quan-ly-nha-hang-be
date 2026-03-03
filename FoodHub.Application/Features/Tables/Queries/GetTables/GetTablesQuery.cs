using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Queries.GetTables
{
    public record GetTablesQuery(PaginationParams Pagination, Guid? AreaId): IRequest<Result<PagedResult<GetTablesResponse>>>;
}
