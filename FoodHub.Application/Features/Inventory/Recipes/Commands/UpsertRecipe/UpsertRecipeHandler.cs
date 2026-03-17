using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe
{
    public class UpsertRecipeHandler : IRequestHandler<UpsertRecipeCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UpsertRecipeHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Unit>> Handle(
            UpsertRecipeCommand request,
            CancellationToken cancellationToken
        )
        {
            var validationResult = new UpsertRecipeValidator(_messageService).Validate(request);
            if (!validationResult.IsValid)
            {
                return Result<Unit>.Failure(
                    validationResult.Errors.First().ErrorMessage,
                    ResultErrorType.Conflict
                );
            }

            var actorId = _currentUserService.GetUserIdAsGuid();
            var recipeRepo = _unitOfWork.Repository<MenuItemIngredient>();
            var menuItemRepo = _unitOfWork.Repository<MenuItem>();

            var menuItem = await menuItemRepo
                .Query()
                .FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<Unit>.Failure("MenuItem.NotFound", ResultErrorType.NotFound);
            }

            var existing = await recipeRepo
                .Query()
                .Where(x => x.MenuItemId == request.MenuItemId)
                .ToListAsync(cancellationToken);

            var ingredientIds = request.Items.Select(x => x.IngredientId).ToList();

            if (ingredientIds.Count != ingredientIds.Distinct().Count())
            {
                return Result<Unit>.Failure(
                    _messageService.GetMessage(MessageKeys.StockOutReceipt.DuplicateIngredient),
                    ResultErrorType.Conflict
                );
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Remove deleted lines
                var toRemove = existing
                    .Where(e => !ingredientIds.Contains(e.IngredientId))
                    .ToList();
                foreach (var rem in toRemove)
                {
                    recipeRepo.Delete(rem);
                }

                // Upsert items
                foreach (var item in request.Items)
                {
                    var line = existing.FirstOrDefault(x => x.IngredientId == item.IngredientId);
                    if (line == null)
                    {
                        await recipeRepo.AddAsync(
                            MenuItemIngredient.Create(
                                request.MenuItemId,
                                item.IngredientId,
                                item.QuantityPerServing,
                                item.BaseUnit,
                                actorId
                            )
                        );
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
                            await _unitOfWork.RollbackTransactionAsync();
                            return Result<Unit>.Failure(
                                _messageService.GetMessage(MessageKeys.StockOutReceipt.QuantityMin),
                                ResultErrorType.Conflict
                            );
                        }

                        recipeRepo.Update(line);
                    }
                }

                // Update MenuItem Metadata
                menuItem.Description = request.Instructions;
                menuItem.ExpectedTime = request.PrepTimeMinutes;

                // Calculate and update CostPrice
                var allIngredients = await _unitOfWork
                    .Repository<Ingredient>()
                    .Query()
                    .Where(x => ingredientIds.Contains(x.IngredientId))
                    .ToListAsync(cancellationToken);

                decimal totalCost = 0;
                foreach (var item in request.Items)
                {
                    var ing = allIngredients.FirstOrDefault(x =>
                        x.IngredientId == item.IngredientId
                    );
                    if (ing != null)
                    {
                        totalCost += ing.CostPrice * item.QuantityPerServing;
                    }
                }

                menuItem.CostPrice = totalCost;
                menuItemRepo.Update(menuItem);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                return Result<Unit>.Success(Unit.Value);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
