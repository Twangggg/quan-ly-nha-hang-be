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

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck
{
    public class CreateInventoryCheckHandler
        : IRequestHandler<CreateInventoryCheckCommand, Result<CreateInventoryCheckResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateInventoryCheckHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateInventoryCheckHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            ILogger<CreateInventoryCheckHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<CreateInventoryCheckResponse>> Handle(
            CreateInventoryCheckCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling CreateInventoryCheck with {ItemCount} items on {CheckDate}",
                request.Items.Count,
                request.CheckDate
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var ingredientIds = request.Items.Select(x => x.IngredientId).Distinct().ToList();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();

            var ingredients = await ingredientRepo
                .Query()
                .AsNoTracking()
                .Where(x => ingredientIds.Contains(x.IngredientId) && x.IsActive)
                .ToListAsync(cancellationToken);

            if (ingredients.Count != ingredientIds.Count)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                );
            }

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);
            var inventoryCheck = InventoryCheck.Create(request.CheckDate.ToUtc(), actorId);

            foreach (var item in request.Items)
            {
                var ingredient = ingredientMap[item.IngredientId];
                var addItemResult = inventoryCheck.AddItem(
                    item.IngredientId,
                    ingredient.CurrentStock,
                    item.PhysicalQuantity,
                    item.Reason,
                    actorId
                );

                if (!addItemResult.IsSuccess)
                {
                    throw new BusinessException(
                        _messageService.GetMessage(
                            addItemResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                        )
                    );
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.Repository<InventoryCheck>().AddAsync(inventoryCheck);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling CreateInventoryCheck with InventoryCheckId={InventoryCheckId}",
                    inventoryCheck.InventoryCheckId
                );

                return Result<CreateInventoryCheckResponse>.Success(
                    new CreateInventoryCheckResponse
                    {
                        InventoryCheckId = inventoryCheck.InventoryCheckId,
                        CheckDate = inventoryCheck.CheckDate,
                        Status = InventoryCheckStatus.Draft,
                        TotalItems = inventoryCheck.Items.Count,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "CreateInventoryCheck transaction rolled back");
                throw;
            }
        }
    }
}
