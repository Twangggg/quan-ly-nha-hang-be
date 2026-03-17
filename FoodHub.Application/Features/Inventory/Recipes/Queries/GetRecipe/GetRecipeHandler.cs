using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe
{
    public class GetRecipeHandler : IRequestHandler<GetRecipeQuery, Result<List<GetRecipeItemResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRecipeHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<GetRecipeItemResponse>>> Handle(GetRecipeQuery request, CancellationToken cancellationToken)
        {
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
                })
                .ToListAsync(cancellationToken);

            return Result<List<GetRecipeItemResponse>>.Success(items);
        }
    }
}
