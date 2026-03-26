using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Groups.Commands.CreateInventoryGroup
{
    public sealed class CreateInventoryGroupHandler
        : IRequestHandler<CreateInventoryGroupCommand, Result<CreateInventoryGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public CreateInventoryGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<CreateInventoryGroupResponse>> Handle(
            CreateInventoryGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            var name = request.Name.Trim();
            var repo = _unitOfWork.Repository<InventoryGroup>();

            var exists = await repo.AnyAsync(x => x.DeletedAt == null && x.Name == name);
            if (exists)
            {
                return Result<CreateInventoryGroupResponse>.Failure(
                    _messageService.GetMessage("InventoryGroup.NameExists") ?? "InventoryGroup.NameExists"
                );
            }

            var group = InventoryGroup.Create(
                name,
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.LowStockThreshold,
                request.ExpiryWarningDays,
                request.DefaultCostMethod
            );

            await repo.AddAsync(group);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<CreateInventoryGroupResponse>.Success(Map(group));
        }

        private static CreateInventoryGroupResponse Map(InventoryGroup group)
        {
            return new CreateInventoryGroupResponse
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
