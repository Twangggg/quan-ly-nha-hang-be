using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemHandler : IRequestHandler<CreateMenuItemCommand, Result<CreateMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateMenuItemHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateMenuItemResponse>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            // 1. Check if Code already exists
            var existingMenuItem = await menuItemRepository.AnyAsync(x => x.Code == request.Code);
            if (existingMenuItem)
            {
                return Result<CreateMenuItemResponse>.Failure($"Menu item with code '{request.Code}' already exists.", ResultErrorType.Conflict);
            }

            // 2. Check if Category exists
            var categoryRepository = _unitOfWork.Repository<Category>();
            var category = await categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<CreateMenuItemResponse>.Failure($"Category with ID '{request.CategoryId}' not found.", ResultErrorType.NotFound);
            }

            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            // 3. Create MenuItem entity
            var menuItem = new MenuItem
            {
                Code = request.Code,
                Name = request.Name,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                CategoryId = request.CategoryId,
                Station = request.Station,
                ExpectedTime = request.ExpectedTime ?? 0,
                PriceDineIn = request.PriceDineIn,
                PriceTakeAway = request.PriceTakeAway ?? request.PriceDineIn, // Default to DineIn price if not set
                CostPrice = request.Cost ?? 0,
                IsOutOfStock = false,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = auditorId,
                UpdatedByEmployeeId = auditorId
            };

            // 4. Save to database
            await menuItemRepository.AddAsync(menuItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 5. Return Response
            var response = new CreateMenuItemResponse
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
                Cost = menuItem.CostPrice,
                IsOutOfStock = menuItem.IsOutOfStock,
                CreatedAt = menuItem.CreatedAt,
                UpdatedAt = menuItem.UpdatedAt
            };

            return Result<CreateMenuItemResponse>.Success(response);
        }
    }
}
