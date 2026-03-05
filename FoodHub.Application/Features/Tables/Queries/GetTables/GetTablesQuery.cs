using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Queries.GetTables
{
    public record GetTablesQuery(Guid? AreaId = null) : IRequest<Result<List<GetTablesResponse>>>;
}

