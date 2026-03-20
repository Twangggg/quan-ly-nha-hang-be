using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
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

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.DeactivateIngredient
{
    public class DeactivateIngredientHandler
        : IRequestHandler<DeactivateIngredientCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeactivateIngredientHandler> _logger;

        public DeactivateIngredientHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<DeactivateIngredientHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(
            DeactivateIngredientCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling DeactivateIngredient for {IngredientId}",
                request.IngredientId
            );

            try
            {
                var repo = _unitOfWork.Repository<Ingredient>();

                var ingredient = await repo.Query()
                    .FirstOrDefaultAsync(
                        x => x.IngredientId == request.IngredientId,
                        cancellationToken
                    );

                if (ingredient == null)
                {
                    _logger.LogWarning("Ingredient {IngredientId} not found", request.IngredientId);
                    return Result<Unit>.NotFound(
                        _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                    );
                }

                // TODO: In the future, check if used in any Recipe
                bool isUsedInRecipe = false;

                var result = ingredient.Deactivate(isUsedInRecipe);

                if (!result.IsSuccess)
                {
                    return Result<Unit>.Failure(
                        _messageService.GetMessage(result.ErrorCode!) ?? result.ErrorCode!
                    );
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling DeactivateIngredient for {IngredientId}",
                    request.IngredientId
                );
                return Result<Unit>.Success(Unit.Value);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while deactivating ingredient {IngredientId}",
                    request.IngredientId
                );
                throw;
            }
        }
    }
}
