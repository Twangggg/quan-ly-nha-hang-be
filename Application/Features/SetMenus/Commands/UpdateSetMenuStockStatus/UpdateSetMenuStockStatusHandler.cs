using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public class UpdateSetMenuStockStatusHandler : IRequestHandler<UpdateSetMenuStockStatusCommand, Result<UpdateSetMenuStockStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSetMenuStockStatusHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateSetMenuStockStatusResponse>> Handle(UpdateSetMenuStockStatusCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();

            // 1. Get existing SetMenu
            var setMenu = await setMenuRepository.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<UpdateSetMenuStockStatusResponse>.Failure($"Set Menu with ID '{request.SetMenuId}' not found.", ResultErrorType.NotFound);
            }

            // 2. Update stock status
            setMenu.IsOutOfStock = request.IsOutOfStock;
            setMenu.UpdatedAt = DateTime.UtcNow;

            // 3. Save changes
            setMenuRepository.Update(setMenu);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 4. Return Response
            var response = new UpdateSetMenuStockStatusResponse
            {
                SetMenuId = setMenu.SetMenuId,
                IsOutOfStock = setMenu.IsOutOfStock,
                UpdatedAt = setMenu.UpdatedAt ?? DateTime.UtcNow
            };

            return Result<UpdateSetMenuStockStatusResponse>.Success(response);
        }
    }
}

