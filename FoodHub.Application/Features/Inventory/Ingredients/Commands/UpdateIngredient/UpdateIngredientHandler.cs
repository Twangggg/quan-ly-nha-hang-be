using System;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient
{
    public class UpdateIngredientHandler
        : IRequestHandler<UpdateIngredientCommand, Result<UpdateIngredientResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateIngredientHandler> _logger;

        public UpdateIngredientHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<UpdateIngredientHandler> logger,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UpdateIngredientResponse>> Handle(
            UpdateIngredientCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling UpdateIngredient for {IngredientId}",
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
                    return Result<UpdateIngredientResponse>.NotFound(
                        _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                    );
                }

                // Check code duplicate (excluding current)
                var codeExists = await repo.AnyAsync(x =>
                    x.Code.ToLower() == request.Code.ToLower()
                    && x.IngredientId != request.IngredientId
                );

                if (codeExists)
                {
                    return Result<UpdateIngredientResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Ingredient.CodeExists),
                        ResultErrorType.Conflict
                    );
                }

                // Check name duplicate (excluding current)
                var nameExists = await repo.AnyAsync(x =>
                    x.Name.ToLower() == request.Name.ToLower()
                    && x.IngredientId != request.IngredientId
                );

                if (nameExists)
                {
                    return Result<UpdateIngredientResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Ingredient.NameExists),
                        ResultErrorType.Conflict
                    );
                }

                // Disallow direct edits of quantity and cost; those are controlled via stock flows
                // (e.g., receiving or adjustments). Keep existing values intact here.
                Guid? auditorId = null;
                if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
                {
                    auditorId = parsedUserId;
                }
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    ingredient.Update(
                        request.Name,
                        request.Unit,
                        request.LowStockThreshold,
                        request.Description,
                        request.IsActive,
                        request.Code,
                        ingredient.CurrentStock,
                        ingredient.CostPrice,
                        auditorId
                    );

                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();

                    var response = new UpdateIngredientResponse
                    {
                        IngredientId = ingredient.IngredientId,
                        Code = ingredient.Code,
                        Name = ingredient.Name,
                        Unit = ingredient.Unit,
                        LowStockThreshold = ingredient.LowStockThreshold,
                        CurrentStock = ingredient.CurrentStock,
                        CostPrice = ingredient.CostPrice,
                        StockStatus = ingredient.GetStockStatus(),
                        IsActive = ingredient.IsActive,
                        Description = ingredient.Description,
                        UpdatedAt = ingredient.UpdatedAt,
                        UpdatedBy = ingredient.UpdatedBy,
                    };

                    _logger.LogInformation(
                        "End handling UpdateIngredient for {IngredientId}",
                        request.IngredientId
                    );
                    return Result<UpdateIngredientResponse>.Success(response);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while updating ingredient {IngredientId}",
                    request.IngredientId
                );
                throw;
            }
        }
    }
}
