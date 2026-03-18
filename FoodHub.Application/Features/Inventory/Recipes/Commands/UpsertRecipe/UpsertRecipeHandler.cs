using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
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

namespace FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe
{
    public class UpsertRecipeHandler : IRequestHandler<UpsertRecipeCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpsertRecipeHandler> _logger;

        public UpsertRecipeHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpsertRecipeHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            UpsertRecipeCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Starting UpsertRecipe for MenuItemId: {MenuItemId}",
                request.MenuItemId
            );

            var validationResult = new UpsertRecipeValidator(_messageService).Validate(request);
            if (!validationResult.IsValid)
            {
                return Result<Guid>.Failure(
                    validationResult.Errors.First().ErrorMessage,
                    ResultErrorType.Conflict
                );
            }

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
                return Result<Guid>.Failure("MenuItem.NotFound", ResultErrorType.NotFound);
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
                // Remove deleted lines
                var toRemove = menuItem
                    .Ingredients.Where(e => !ingredientIds.Contains(e.IngredientId))
                    .ToList();
                foreach (var rem in toRemove)
                {
                    menuItem.Ingredients.Remove(rem);
                    recipeRepo.Delete(rem);
                }

                // Upsert items
                foreach (var item in request.Items)
                {
                    var line = menuItem.Ingredients.FirstOrDefault(x =>
                        x.IngredientId == item.IngredientId
                    );
                    if (line == null)
                    {
                        var newLine = MenuItemIngredient.Create(
                            request.MenuItemId,
                            item.IngredientId,
                            item.QuantityPerServing,
                            item.BaseUnit,
                            actorId
                        );
                        await recipeRepo.AddAsync(newLine);
                        menuItem.Ingredients.Add(newLine);
                    }
                    else
                    {
                        var updateResult = line.Update(
                            item.QuantityPerServing,
                            item.BaseUnit,
                            actorId
                        );
                        if (!updateResult.IsSuccess)
                        {
                            throw new FoodHub.Application.Common.Exceptions.BusinessException(
                                updateResult.ErrorCode
                                    ?? _messageService.GetMessage(
                                        MessageKeys.StockOutReceipt.QuantityMin
                                    )
                            );
                        }

                        recipeRepo.Update(line);
                    }
                }

                // Update MenuItem Metadata
                menuItem.Description = request.Instructions;
                menuItem.ExpectedTime = request.PrepTimeMinutes;

                // Calculate and update CostPrice via Domain Entity
                // ingredients are already in memory and navigation properties are fixed-up by EF Core
                menuItem.UpdateCostFromIngredients(menuItem.Ingredients);

                _unitOfWork.Repository<MenuItem>().Update(menuItem);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

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
