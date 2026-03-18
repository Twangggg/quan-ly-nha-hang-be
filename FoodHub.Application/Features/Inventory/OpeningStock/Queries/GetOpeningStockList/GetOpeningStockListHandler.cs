using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.OpeningStock.Queries.GetOpeningStockList
{
    public class GetOpeningStockListHandler
        : IRequestHandler<
            GetOpeningStockListQuery,
            Result<PagedResult<GetOpeningStockListResponse>>
        >
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetOpeningStockListHandler> _logger;

        public GetOpeningStockListHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetOpeningStockListHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetOpeningStockListResponse>>> Handle(
            GetOpeningStockListQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetOpeningStockList with PageNumber={PageNumber}, PageSize={PageSize}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var query = _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new GetOpeningStockListResponse
                {
                    IngredientId = x.IngredientId,
                    Code = x.Code,
                    Name = x.Name,
                    Unit = x.BaseUnit,
                    CurrentStock = x.CurrentStock,
                    CostPrice = x.CostPrice,
                });

            var pagedResult = await query.ToPagedResultAsync(request.Pagination, cancellationToken);

            _logger.LogInformation(
                "End handling GetOpeningStockList with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            return Result<PagedResult<GetOpeningStockListResponse>>.Success(pagedResult);
        }
    }
}
