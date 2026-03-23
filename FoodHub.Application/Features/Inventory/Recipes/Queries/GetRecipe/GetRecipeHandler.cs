using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe
{
    public class GetRecipeHandler : IRequestHandler<GetRecipeQuery, Result<GetRecipeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetRecipeHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<Result<GetRecipeResponse>> Handle(
            GetRecipeQuery request,
            CancellationToken cancellationToken
        )
        {
            var cacheKey = string.Format(CacheKey.InventoryRecipeByMenuItem, request.MenuItemId);
            var cached = await _cacheService.GetAsync<GetRecipeResponse>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                return Result<GetRecipeResponse>.Success(cached);
            }

            var menuItem = await _unitOfWork
                .Repository<Domain.Entities.MenuItem>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<GetRecipeResponse>.Failure(
                    "MenuItem.NotFound",
                    ResultErrorType.NotFound
                );
            }

            var items = await _unitOfWork
                .Repository<Domain.Entities.MenuItemIngredient>()
                .Query()
                .AsNoTracking()
                .Where(x => x.MenuItemId == request.MenuItemId)
                .Include(x => x.Ingredient)
                .Select(x => new GetRecipeItemResponse
                {
                    IngredientId = x.IngredientId,
                    IngredientName = x.Ingredient.Name,
                    BaseUnit = x.Ingredient.BaseUnit,
                    QuantityPerServing = x.QuantityPerServing,
                    CostPrice = x.Ingredient.CostPrice,
                    TotalCost = x.Ingredient.CostPrice * x.QuantityPerServing,
                })
                .ToListAsync(cancellationToken);

            var response = new GetRecipeResponse
            {
                MenuItemId = menuItem.MenuItemId,
                Instructions = menuItem.Description,
                PrepTimeMinutes = menuItem.ExpectedTime,
                TotalCost = menuItem.CostPrice,
                Items = items,
            };

            await _cacheService.SetAsync(
                cacheKey,
                response,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<GetRecipeResponse>.Success(response);
        }
    }
}
