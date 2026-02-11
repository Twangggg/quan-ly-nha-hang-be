using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu
{
    public class DeleteSetMenuHandler : IRequestHandler<DeleteSetMenuCommand, Result<DeleteSetMenuResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public DeleteSetMenuHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<DeleteSetMenuResponse>> Handle(DeleteSetMenuCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();
            var setMenuItemRepository = _unitOfWork.Repository<SetMenuItem>();

            var userRole = _currentUserService.Role;
            if (userRole is not "Manager")
            {
                return Result<DeleteSetMenuResponse>.Failure(_messageService.GetMessage(MessageKeys.SetMenu.DeleteForbidden), ResultErrorType.Forbidden);
            }


            // 1. Get existing SetMenu
            var setMenu = await setMenuRepository.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<DeleteSetMenuResponse>.Failure(_messageService.GetMessage(MessageKeys.SetMenu.NotFound), ResultErrorType.NotFound);
            }

            // 2. Begin Transaction
            setMenu.DeletedAt = DateTime.UtcNow;
            setMenu.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            // 3. Soft delete associated SetMenuItems
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync("setmenu:list", cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.SetMenuById, request.SetMenuId), cancellationToken);

            // 6. Return Response
            var updatedItems = await setMenuItemRepository.Query()
                .Where(x => x.SetMenuId == request.SetMenuId)
                .ToListAsync(cancellationToken);
            var response = new DeleteSetMenuResponse
            {
                SetMenuId = setMenu.SetMenuId,
                Code = setMenu.Code,
                Name = setMenu.Name,
                SetType = setMenu.SetType,
                ImageUrl = setMenu.ImageUrl,
                Description = setMenu.Description,
                CostPrice = setMenu.CostPrice,
                Price = setMenu.Price,
                UpdatedAt = setMenu.UpdatedAt ?? DateTime.UtcNow,
                Items = updatedItems.Select(item => new DeleteSetMenuItemResponse
                {
                    SetMenuItemId = item.SetMenuItemId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    CreatedAt = item.CreatedAt
                }).ToList()
            };

            return Result<DeleteSetMenuResponse>.Success(response);

        }
    }
}
