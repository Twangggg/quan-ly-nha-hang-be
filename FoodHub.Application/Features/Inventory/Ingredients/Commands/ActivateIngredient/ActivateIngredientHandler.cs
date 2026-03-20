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

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.ActivateIngredient
{
    public class ActivateIngredientHandler
        : IRequestHandler<ActivateIngredientCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ActivateIngredientHandler> _logger;

        public ActivateIngredientHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<ActivateIngredientHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(
            ActivateIngredientCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling ActivateIngredient for {IngredientId}",
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

                ingredient.Activate();

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling ActivateIngredient for {IngredientId}",
                    request.IngredientId
                );
                return Result<Unit>.Success(Unit.Value);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while activating ingredient {IngredientId}",
                    request.IngredientId
                );
                throw;
            }
        }
    }
}
