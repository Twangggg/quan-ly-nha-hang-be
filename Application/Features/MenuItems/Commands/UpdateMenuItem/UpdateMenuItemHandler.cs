using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemHandler : IRequestHandler<UpdateMenuItemCommand, Result<UpdateMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        public UpdateMenuItemHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UpdateMenuItemResponse>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<MenuItem>();

            var menuItem = await repo.Query()
                .Include(mi => mi.Category)
                .FirstOrDefaultAsync(mi => mi.MenuItemId == request.MenuItemId, cancellationToken);
            if (menuItem is null) return Result<UpdateMenuItemResponse>.NotFound("Menu item is not found!");

            var name = request.Name.Trim();
            var imageUrl = request.ImageUrl.Trim();
            var description = request.Description?.Trim();
            var categoryId = request.CategoryId;
            var station = request.Station;
            var expectedTime = request.ExpectedTime;
            var priceDineIn = request.PriceDineIn;
            var priceTakeAway = request.PriceTakeAway;
            var costPrice = request.CostPrice;

            var categoryExists = await _unitOfWork.Repository<Category>().Query()
                .AnyAsync(c => c.CategoryId == categoryId, cancellationToken);
            if (!categoryExists)
                return Result<UpdateMenuItemResponse>.Failure("Category is not exist!");

            menuItem.Name = name;
            menuItem.ImageUrl = imageUrl;
            menuItem.Description = description;
            menuItem.CategoryId = categoryId;
            menuItem.Station = station;
            menuItem.ExpectedTime = expectedTime;
            menuItem.PriceDineIn = priceDineIn;
            menuItem.PriceTakeAway = priceTakeAway;

            menuItem.UpdatedAt = DateTime.UtcNow;
            menuItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            if (costPrice.HasValue)
            {
                if (_currentUserService.Role is EmployeeRole.Manager or EmployeeRole.Cashier)
                    menuItem.CostPrice = costPrice.Value;
                else return Result<UpdateMenuItemResponse>.Failure("You do not have permission to update the cost of the menu item!", ResultErrorType.Forbidden);
            }

            await _unitOfWork.SaveChangeAsync();

            var response = _mapper.Map<UpdateMenuItemResponse>(menuItem);
            return Result<UpdateMenuItemResponse>.Success(response);
        }
    }
}
