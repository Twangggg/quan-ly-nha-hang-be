using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock
{
    public class UpdateMenuItemStockStatusHandler : IRequestHandler<UpdateMenuItemStockStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public UpdateMenuItemStockStatusHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<bool>> Handle(UpdateMenuItemStockStatusCommand request, CancellationToken cancellationToken)
        {
            // Get the repository for MenuItem
            var repo = _unitOfWork.Repository<MenuItem>();

            if (_currentUserService.Role is not EmployeeRole.Manager)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.UpdateStockForbidden), ResultErrorType.Forbidden);
            }

            // Check if the menu item exists
            var menuItem = await repo.Query()
                .FirstOrDefaultAsync(mi => mi.MenuItemId == request.MenuItemId, cancellationToken);
            if (menuItem is null) return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound), ResultErrorType.NotFound);

            // Toggle the out of stock status
            menuItem.IsOutOfStock = request.IsOutOfStock;
            menuItem.UpdatedAt = DateTime.UtcNow;
            menuItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync();

            return Result<bool>.Success(true);
        }
    }
}
