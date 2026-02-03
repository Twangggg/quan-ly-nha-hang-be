using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemHandler : IRequestHandler<CreateMenuItemCommand, Result<MenuItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateMenuItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<MenuItemDto>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            // 1. Check if Code already exists
            var existingMenuItem = await menuItemRepository.AnyAsync(x => x.Code == request.Code);
            if (existingMenuItem)
            {
                return Result<MenuItemDto>.Failure($"Menu item with code '{request.Code}' already exists.");
            }

            // 2. Check if Category exists
            var categoryRepository = _unitOfWork.Repository<Category>();
            var category = await categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<MenuItemDto>.Failure($"Category with ID '{request.CategoryId}' not found.", ResultErrorType.NotFound);
            }

            // 3. Create MenuItem entity
            var menuItem = new MenuItem
            {
                Code = request.Code,
                Name = request.Name,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                CategoryId = request.CategoryId,
                Station = (Station)request.Station,
                ExpectedTime = request.ExpectedTime ?? 0,
                PriceDineIn = request.PriceDineIn,
                PriceTakeAway = request.PriceTakeAway ?? request.PriceDineIn, // Default to DineIn price if not set
                Cost = request.Cost ?? 0,
                IsOutOfStock = false
            };

            // 4. Save to database
            await menuItemRepository.AddAsync(menuItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 5. Return DTO
            var dto = new MenuItemDto
            {
                MenuItemId = menuItem.MenuItemId,
                Code = menuItem.Code,
                Name = menuItem.Name,
                ImageUrl = menuItem.ImageUrl,
                Description = menuItem.Description,
                CategoryId = menuItem.CategoryId,
                CategoryName = category.Name,
                Station = (int)menuItem.Station,
                ExpectedTime = menuItem.ExpectedTime,
                PriceDineIn = menuItem.PriceDineIn,
                PriceTakeAway = menuItem.PriceTakeAway,
                Cost = menuItem.Cost,
                IsOutOfStock = menuItem.IsOutOfStock,
                CreatedAt = menuItem.CreatedAt,
                UpdatedAt = menuItem.UpdatedAt
            };

            return Result<MenuItemDto>.Success(dto);
        }
    }
}
