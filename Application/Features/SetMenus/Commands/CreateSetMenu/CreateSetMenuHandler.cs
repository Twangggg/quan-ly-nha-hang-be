using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuHandler : IRequestHandler<CreateSetMenuCommand, Result<CreateSetMenuResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSetMenuHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            // 3. Create SetMenu
            var setMenu = new SetMenu
            {
                SetMenuId = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                Price = request.Price,
                IsOutOfStock = false,
                SetMenuItems = request.Items.Select(itemRequest => new SetMenuItem
                {
                    MenuItemId = itemRequest.MenuItemId,
                    Quantity = itemRequest.Quantity
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
                Price = setMenu.Price,
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
