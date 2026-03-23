using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm
{
    public class GetInventoryCheckCreateFormHandler
        : IRequestHandler<
            GetInventoryCheckCreateFormQuery,
            Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>
        >
    {
        private readonly ILogger<GetInventoryCheckCreateFormHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetInventoryCheckCreateFormHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventoryCheckCreateFormHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>> Handle(
            GetInventoryCheckCreateFormQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetInventoryCheckCreateForm");

            var cacheKey = CacheKey.InventoryCheckCreateForm;
            var cached = await _cacheService.GetAsync<IReadOnlyList<GetInventoryCheckCreateFormResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryCheckCreateForm with {Count} items (from cache)",
                    cached.Count
                );
                return Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>.Success(cached);
            }

            var items = await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new GetInventoryCheckCreateFormResponse
                {
                    IngredientId = x.IngredientId,
                    IngredientCode = x.Code,
                    IngredientName = x.Name,
                    BaseUnit = x.BaseUnit,
                    BookQuantity = x.CurrentStock,
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "End handling GetInventoryCheckCreateForm with {Count} items",
                items.Count
            );

            await _cacheService.SetAsync(
                cacheKey,
                items,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>.Success(items);
        }
    }
}
