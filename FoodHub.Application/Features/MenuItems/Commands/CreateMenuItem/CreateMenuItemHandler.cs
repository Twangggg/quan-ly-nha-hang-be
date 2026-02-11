using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemHandler
        : IRequestHandler<CreateMenuItemCommand, Result<CreateMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateMenuItemHandler> _logger;

        //private readonly ICloudinaryService _cloudinaryService;

        public CreateMenuItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<CreateMenuItemHandler> logger
        ) //, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
            //_cloudinaryService = cloudinaryService;
        }

        public async Task<Result<CreateMenuItemResponse>> Handle(
            CreateMenuItemCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Creating new menu item: {Name} (Code: {Code})",
                request.Name,
                request.Code
            );
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            // 1. Check if Code already exists
            var existingMenuItem = await menuItemRepository.AnyAsync(x => x.Code == request.Code);
            if (existingMenuItem)
            {
                _logger.LogWarning(
                    "Failed to create menu item. Code {Code} already exists.",
                    request.Code
                );
                return Result<CreateMenuItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.CodeExists, request.Code),
                    ResultErrorType.Conflict
                );
            }

            // 2. Check if Category exists
            var categoryRepository = _unitOfWork.Repository<Category>();
            var category = await categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
                return Result<CreateMenuItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Category.NotFound, request.CategoryId),
                    ResultErrorType.NotFound
                );
            }

            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            // 3. Handle Image Upload if provided
            var imageUrl = request.ImageUrl;
            //if (request.ImageFile != null)
            //{
            //    try
            //    {
            //        imageUrl = await _cloudinaryService.UploadImageAsync(request.ImageFile, "menu-items");
            //    }
            //    catch (Exception ex)
            //    {
            //        return Result<CreateMenuItemResponse>.Failure($"Image upload failed: {ex.Message}", ResultErrorType.BadRequest);
            //    }
            //}

            // 4. Create MenuItem entity
            var menuItem = new MenuItem
            {
                MenuItemId = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                ImageUrl = imageUrl ?? "",
                Description = request.Description,
                CategoryId = request.CategoryId,
                Station = request.Station,
                ExpectedTime = request.ExpectedTime,
                PriceDineIn = request.PriceDineIn,
                PriceTakeAway = request.PriceTakeAway ?? request.PriceDineIn, // Default to DineIn price if not set
                CostPrice = request.Cost ?? 0,
                IsOutOfStock = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = auditorId,
                UpdatedBy = auditorId,
            };

            // 4. Save to database
            await menuItemRepository.AddAsync(menuItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync("menuitem:list", cancellationToken);

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
                UpdatedAt = menuItem.UpdatedAt,
            };

            return Result<CreateMenuItemResponse>.Success(response);
        }
    }
}
