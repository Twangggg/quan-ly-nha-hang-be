using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Resources;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public class UpdateStockStatusHandler : IRequestHandler<UpdateStockStatusCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ErrorMessages> _localizer;

        public UpdateStockStatusHandler(IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        public async Task<Result<Unit>> Handle(UpdateStockStatusCommand request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<Unit>.Failure(_localizer["MenuItem.NotFound", request.MenuItemId].Value);
            }

            // Toggle stock status
            menuItem.IsOutOfStock = request.IsOutOfStock;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
