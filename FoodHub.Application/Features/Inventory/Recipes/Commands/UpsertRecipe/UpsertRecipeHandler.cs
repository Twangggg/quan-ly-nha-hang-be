using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe
{
    public class UpsertRecipeHandler : IRequestHandler<UpsertRecipeCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UpsertRecipeHandler> _logger;

        public UpsertRecipeHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            ILogger<UpsertRecipeHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            UpsertRecipeCommand request,
            CancellationToken cancellationToken
        )
        {
            var validationResult = new UpsertRecipeValidator(_messageService).Validate(request);
            if (!validationResult.IsValid)
            {
                return Result<Guid>.Failure(
                    validationResult.Errors.First().ErrorMessage,
                    ResultErrorType.Conflict
                );
            }

            _logger.LogInformation(
                "Starting UpsertRecipe for MenuItemId: {MenuItemId}",
                request.MenuItemId
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var recipeRepo = _unitOfWork.Repository<MenuItemIngredient>();
            var menuItemRepo = _unitOfWork.Repository<MenuItem>();

            var menuItem = await menuItemRepo
                .Query()
                .Include(m => m.Ingredients)
                    .ThenInclude(i => i.Ingredient)
                .FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound),
                    ResultErrorType.NotFound
                );
            }

            var ingredientIds = request.Items.Select(x => x.IngredientId).ToList();

            // Fetch all required ingredients to ensure they are tracked for Identity Resolution (Fix-up)
            // and cost calculation in the loop/domain method.
            await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .ToListAsync(cancellationToken);

            if (ingredientIds.Count != ingredientIds.Distinct().Count())
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.StockOutReceipt.DuplicateIngredient),
                    ResultErrorType.Conflict
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var recipeItems = request.Items.Select(i => new MenuItem.RecipeItemInput(
                    i.IngredientId,
                    i.QuantityPerServing,
                    i.BaseUnit
                ));

                var updateResult = menuItem.UpdateRecipe(
                    recipeItems,
                    request.Instructions,
                    request.PrepTimeMinutes,
                    actorId
                );

                if (!updateResult.IsSuccess)
                {
                    throw new FoodHub.Application.Common.Exceptions.BusinessException(
                        updateResult.ErrorCode
                            ?? _messageService.GetMessage(MessageKeys.StockOutReceipt.QuantityMin)
                    );
                }

                foreach (var added in updateResult.Value!.Added)
                {
                    await recipeRepo.AddAsync(added);
                }

                foreach (var updated in updateResult.Value!.Updated)
                {
                    recipeRepo.Update(updated);
                }

                foreach (var removed in updateResult.Value!.Removed)
                {
                    recipeRepo.Delete(removed);
                }

                _unitOfWork.Repository<MenuItem>().Update(menuItem);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "Successfully updated recipe for MenuItemId: {MenuItemId}. New Cost: {TotalCost}",
                    request.MenuItemId,
                    menuItem.CostPrice
                );

                return Result<Guid>.Success(menuItem.MenuItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while upserting recipe for MenuItemId: {MenuItemId}",
                    request.MenuItemId
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
