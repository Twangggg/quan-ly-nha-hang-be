using FoodHub.Application.Common.Models;
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

        public UpdateSetMenuStockStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(UpdateSetMenuStockStatusCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepo = _unitOfWork.Repository<SetMenu>();

            // Authorization: Only Managers can update stock status
            var userRole = _currentUserService.Role;
            if (userRole is not EmployeeRole.Manager)
            {
                return Result<bool>.Failure("You do not have permission to update set menu stock status!", ResultErrorType.Forbidden);
            }

            // 1. Get existing SetMenu
            var setMenu = await setMenuRepo.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<bool>.Failure($"Set Menu with ID '{request.SetMenuId}' not found.", ResultErrorType.NotFound);
            }

            // 2. Update stock status
            setMenu.IsOutOfStock = request.IsOutOfStock;
            setMenu.UpdatedAt = DateTime.UtcNow;

            // 3. Save changes
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 4. Return Response
            return Result<bool>.Success(true);
        }
    }
}

