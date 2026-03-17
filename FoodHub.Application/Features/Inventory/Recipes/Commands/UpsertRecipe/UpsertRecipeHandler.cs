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
                var toRemove = existing.Where(e => !ingredientIds.Contains(e.IngredientId)).ToList();
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
                                actorId
                            )
                        );
                    }
                    else
                    {
                        var updateResult = line.UpdateQuantity(item.QuantityPerServing, actorId);
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
