using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuHandler : IRequestHandler<UpdateSetMenuCommand, Result<UpdateSetMenuResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSetMenuHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateSetMenuResponse>> Handle(UpdateSetMenuCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();
            var setMenuItemRepository = _unitOfWork.Repository<SetMenuItem>();

            // 1. Get existing SetMenu
            var setMenu = await setMenuRepository.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<UpdateSetMenuResponse>.Failure($"Set Menu with ID '{request.SetMenuId}' not found.", ResultErrorType.NotFound);
            }

            // 2. Validate if all MenuItems exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var existingMenuItemsCount = await menuItemRepository.CountAsync(mi => menuItemIds.Contains(mi.MenuItemId));

            if (existingMenuItemsCount != menuItemIds.Count)
            {
                return Result<UpdateSetMenuResponse>.Failure("One or more Menu Items do not exist.", ResultErrorType.BadRequest);
            }

            // 3. Update SetMenu properties
            setMenu.Name = request.Name;
            setMenu.Price = request.Price;
            setMenu.UpdatedAt = DateTime.UtcNow;

            // 4. Update SetMenuItems - remove old and add new
            var existingItems = setMenuItemRepository.Query().Where(smi => smi.SetMenuId == request.SetMenuId).ToList();
            foreach (var item in existingItems)
            {
                setMenuItemRepository.Delete(item);
            }

            var newItems = request.Items.Select(itemRequest => new SetMenuItem
            {
                SetMenuId = request.SetMenuId,
                MenuItemId = itemRequest.MenuItemId,
                Quantity = itemRequest.Quantity
            }).ToList();

            foreach (var item in newItems)
            {
                await setMenuItemRepository.AddAsync(item);
            }

            // 5. Save changes
            setMenuRepository.Update(setMenu);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 6. Return Response
            var response = new UpdateSetMenuResponse
            {
                SetMenuId = setMenu.SetMenuId,
                Name = setMenu.Name,
                Price = setMenu.Price,
                IsOutOfStock = setMenu.IsOutOfStock,
                UpdatedAt = setMenu.UpdatedAt ?? DateTime.UtcNow,
                Items = newItems.Select(item => new UpdateSetMenuItemResponse
                {
                    SetMenuItemId = item.SetMenuItemId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity
                }).ToList()
            };

            return Result<UpdateSetMenuResponse>.Success(response);
        }
    }
}


