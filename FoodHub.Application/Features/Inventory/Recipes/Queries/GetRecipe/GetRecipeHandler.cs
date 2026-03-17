using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe
{
    public class GetRecipeHandler : IRequestHandler<GetRecipeQuery, Result<GetRecipeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRecipeHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<GetRecipeResponse>> Handle(
            GetRecipeQuery request,
            CancellationToken cancellationToken
        )
        {
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

            return Result<GetRecipeResponse>.Success(response);
        }
    }
}
