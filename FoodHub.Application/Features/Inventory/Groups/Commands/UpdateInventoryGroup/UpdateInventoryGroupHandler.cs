using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Groups.Commands.UpdateInventoryGroup
{
    public sealed class UpdateInventoryGroupHandler
        : IRequestHandler<UpdateInventoryGroupCommand, Result<UpdateInventoryGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public UpdateInventoryGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<UpdateInventoryGroupResponse>> Handle(
            UpdateInventoryGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            var repo = _unitOfWork.Repository<InventoryGroup>();
            var group = await repo.GetByIdAsync(request.InventoryGroupId);

            if (group is null || group.DeletedAt != null)
            {
                return Result<UpdateInventoryGroupResponse>.NotFound(
                    _messageService.GetMessage("InventoryGroup.NotFound") ?? "InventoryGroup.NotFound"
                );
            }

            var name = request.Name.Trim();
            var exists = await repo.AnyAsync(x =>
                x.DeletedAt == null
                && x.InventoryGroupId != request.InventoryGroupId
                && x.Name == name
            );
            if (exists)
            {
                return Result<UpdateInventoryGroupResponse>.Failure(
                    _messageService.GetMessage("InventoryGroup.NameExists") ?? "InventoryGroup.NameExists"
                );
            }

            var result = group.Update(
                name,
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.LowStockThreshold,
                request.ExpiryWarningDays,
                request.DefaultCostMethod
            );

            if (!result.IsSuccess)
            {
                return Result<UpdateInventoryGroupResponse>.Failure(
                    _messageService.GetMessage(result.ErrorCode!) ?? result.ErrorCode!
                );
            }

            repo.Update(group);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<UpdateInventoryGroupResponse>.Success(Map(group));
        }

        private static UpdateInventoryGroupResponse Map(InventoryGroup group)
        {
            return new UpdateInventoryGroupResponse
            {
                InventoryGroupId = group.InventoryGroupId,
                Name = group.Name,
                Description = group.Description,
                LowStockThreshold = group.LowStockThreshold,
                ExpiryWarningDays = group.ExpiryWarningDays,
                DefaultCostMethod = group.DefaultCostMethod,
                IngredientCount = group.Ingredients.Count,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
            };
        }
    }
}
