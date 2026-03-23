using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Lots.Commands.DisposeInventoryLot
{
    public class DisposeInventoryLotHandler
        : IRequestHandler<DisposeInventoryLotCommand, Result<DisposeInventoryLotResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DisposeInventoryLotHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public DisposeInventoryLotHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            ILogger<DisposeInventoryLotHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<DisposeInventoryLotResponse>> Handle(
            DisposeInventoryLotCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling DisposeInventoryLot for LotId={LotId} Quantity={Quantity}",
                request.LotId,
                request.Quantity
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var lotRepo = _unitOfWork.Repository<InventoryLot>();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();
            var movementRepo = _unitOfWork.Repository<InventoryLotMovement>();
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            var lot = await lotRepo
                .Query()
                .FirstOrDefaultAsync(x => x.InventoryLotId == request.LotId, cancellationToken);

            if (lot is null)
            {
                throw new NotFoundException(_messageService.GetMessage(MessageKeys.InventoryLot.NotFound));
            }

            var ingredient = await ingredientRepo
                .Query()
                .FirstOrDefaultAsync(x => x.IngredientId == lot.IngredientId, cancellationToken);

            if (ingredient is null)
            {
                throw new NotFoundException(_messageService.GetMessage(MessageKeys.Ingredient.NotFound));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var disposeResult = lot.MarkDisposed(
                    request.Quantity,
                    request.Reason,
                    DateTime.UtcNow,
                    actorId
                );
                if (!disposeResult.IsSuccess)
                {
                    throw new BusinessException(
                        _messageService.GetMessage(MapLotError(disposeResult.ErrorCode))
                    );
                }

                var reduceStockResult = ingredient.ReduceStock(request.Quantity, actorId);
                if (!reduceStockResult.IsSuccess)
                {
                    throw new BusinessException(
                        _messageService.GetMessage(
                            reduceStockResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                        )
                    );
                }

                await movementRepo.AddAsync(
                    InventoryLotMovement.Create(
                        lot.InventoryLotId,
                        InventoryLotTransactionType.Dispose,
                        -request.Quantity,
                        lot.RemainingQuantity,
                        nameof(InventoryLot),
                        lot.InventoryLotId,
                        lot.LotCode,
                        DateTime.UtcNow,
                        lot.UnitCost,
                        request.Reason,
                        actorId
                    )
                );

                await transactionRepo.AddAsync(
                    InventoryTransaction.CreateStockOut(
                        ingredient.IngredientId,
                        request.Quantity,
                        lot.UnitCost,
                        ingredient.CurrentStock,
                        $"LOT-DISPOSE:{lot.LotCode}",
                        actorId
                    )
                );

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling DisposeInventoryLot for LotId={LotId} RemainingQuantity={RemainingQuantity}",
                    lot.InventoryLotId,
                    lot.RemainingQuantity
                );

                return Result<DisposeInventoryLotResponse>.Success(
                    new DisposeInventoryLotResponse
                    {
                        LotId = lot.InventoryLotId,
                        RemainingQuantity = lot.RemainingQuantity,
                        Status = lot.Status,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "DisposeInventoryLot transaction rolled back");
                throw;
            }
        }

        private static string MapLotError(string? errorCode)
        {
            return errorCode switch
            {
                "InventoryLot.AlreadyDisposed" => MessageKeys.InventoryLot.AlreadyDisposed,
                "InventoryLot.Expired" => MessageKeys.InventoryLot.Expired,
                "InventoryLot.InsufficientQuantity" => MessageKeys.InventoryLot.InsufficientQuantity,
                "InventoryLot.InvalidAdjustment" => MessageKeys.InventoryLot.InvalidAdjustment,
                "InventoryLot.ReasonRequired" => MessageKeys.InventoryLot.ReasonRequired,
                _ => MessageKeys.Common.ValidationFailed,
            };
        }
    }
}
