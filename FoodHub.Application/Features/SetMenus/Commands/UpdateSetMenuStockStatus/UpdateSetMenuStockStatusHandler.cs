using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public class UpdateSetMenuStockStatusHandler : IRequestHandler<UpdateSetMenuStockStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public UpdateSetMenuStockStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<bool>> Handle(UpdateSetMenuStockStatusCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepo = _unitOfWork.Repository<SetMenu>();

            // Authorization: Only Managers can update stock status
            var userRole = _currentUserService.Role;
            if (userRole is not "Manager")
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.SetMenu.UpdateForbidden), ResultErrorType.Forbidden);
            }


            // 1. Get existing SetMenu
            var setMenu = await setMenuRepo.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.SetMenu.NotFound), ResultErrorType.NotFound);
            }

            // 2. Update stock status
            setMenu.IsOutOfStock = request.IsOutOfStock;
            setMenu.UpdatedAt = DateTime.UtcNow;

            // 3. Save changes
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync("setmenu:list", cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.SetMenuById, request.SetMenuId), cancellationToken);

            // 4. Return Response
            return Result<bool>.Success(true);
        }
    }
}
