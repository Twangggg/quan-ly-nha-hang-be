using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuHandler : IRequestHandler<CreateSetMenuCommand, Result<CreateSetMenuResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateSetMenuHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _cloudinaryService = cloudinaryService;
        }


        public async Task<Result<CreateSetMenuResponse>> Handle(CreateSetMenuCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            // 1. Check if Code already exists
            var existingSetMenu = await setMenuRepository.AnyAsync(sm => sm.Code == request.Code);
            if (existingSetMenu)
            {
                return Result<CreateSetMenuResponse>.Failure($"Set Menu with code '{request.Code}' already exists.", ResultErrorType.Conflict);
            }

            // 2. Validate if all MenuItems exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var existingMenuItemsCount = await menuItemRepository.CountAsync(mi => menuItemIds.Contains(mi.MenuItemId));

            if (existingMenuItemsCount != menuItemIds.Count)
            {
                return Result<CreateSetMenuResponse>.Failure("One or more Menu Items do not exist.", ResultErrorType.BadRequest);
            }

            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            // 3. Handle Image Upload if provided
            var imageUrl = request.ImageUrl;
            if (request.ImageFile != null)
            {
                try
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(request.ImageFile, "set-menus");
                }
                catch (Exception ex)
                {
                    return Result<CreateSetMenuResponse>.Failure($"Image upload failed: {ex.Message}", ResultErrorType.BadRequest);
                }
            }

            // 4. Create SetMenu
            var setMenu = new SetMenu
            {
                SetMenuId = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                SetType = request.SetType,
                ImageUrl = imageUrl,
                Description = request.Description,
                Price = request.Price,
                CostPrice = request.CostPrice,
                IsOutOfStock = false,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = auditorId,
                UpdatedByEmployeeId = auditorId,
                SetMenuItems = request.Items.Select(itemRequest => new SetMenuItem
                {
                    SetMenuItemId = Guid.NewGuid(),
                    MenuItemId = itemRequest.MenuItemId,
                    Quantity = itemRequest.Quantity,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };


            // 4. Save to database
            await setMenuRepository.AddAsync(setMenu);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 5. Return Response
            var response = new CreateSetMenuResponse
            {
                SetMenuId = setMenu.SetMenuId,
                Code = setMenu.Code,
                Name = setMenu.Name,
                SetType = setMenu.SetType,
                ImageUrl = setMenu.ImageUrl,
                Description = setMenu.Description,
                Price = setMenu.Price,
                CostPrice = setMenu.CostPrice,
                IsOutOfStock = setMenu.IsOutOfStock,
                CreatedAt = setMenu.CreatedAt,
                UpdatedAt = setMenu.UpdatedAt,
                Items = setMenu.SetMenuItems.Select(item => new SetMenuItemResponse
                {
                    SetMenuItemId = item.SetMenuItemId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity
                }).ToList()
            };


            return Result<CreateSetMenuResponse>.Success(response);
        }
    }
}
