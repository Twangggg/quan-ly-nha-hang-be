using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.Groups.Commands.DeleteInventoryGroup
{
    public sealed class DeleteInventoryGroupHandler
        : IRequestHandler<DeleteInventoryGroupCommand, Result<DeleteInventoryGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public DeleteInventoryGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<DeleteInventoryGroupResponse>> Handle(
            DeleteInventoryGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            var groupRepo = _unitOfWork.Repository<InventoryGroup>();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();

            var group = await groupRepo.GetByIdAsync(request.InventoryGroupId);
            if (group is null || group.DeletedAt != null)
            {
                return Result<DeleteInventoryGroupResponse>.NotFound(
                    _messageService.GetMessage("InventoryGroup.NotFound") ?? "InventoryGroup.NotFound"
                );
            }

            var isUsed = await ingredientRepo.AnyAsync(x => x.InventoryGroupId == request.InventoryGroupId);
            if (isUsed)
            {
                return Result<DeleteInventoryGroupResponse>.Failure(
                    _messageService.GetMessage("InventoryGroup.InUse") ?? "InventoryGroup.InUse"
                );
            }

            groupRepo.Delete(group);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<DeleteInventoryGroupResponse>.Success(
                new DeleteInventoryGroupResponse { InventoryGroupId = request.InventoryGroupId }
            );
        }
    }
}
