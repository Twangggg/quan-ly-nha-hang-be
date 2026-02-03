using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuHandler : IRequestHandler<CreateSetMenuCommand, Result<SetMenuDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSetMenuHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SetMenuDto>> Handle(CreateSetMenuCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            // 1. Check if Code already exists
            var existingSetMenu = await setMenuRepository.AnyAsync(sm => sm.Code == request.Code);
            if (existingSetMenu)
            {
                return Result<SetMenuDto>.Failure($"Set Menu with code '{request.Code}' already exists.", ResultErrorType.Conflict);
            }

            // 2. Validate if all MenuItems exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var existingMenuItemsCount = await menuItemRepository.CountAsync(mi => menuItemIds.Contains(mi.MenuItemId));

            if (existingMenuItemsCount != menuItemIds.Count)
            {
                // Find which ones are missing for better error message? For now generic error.
                return Result<SetMenuDto>.Failure("One or more Menu Items do not exist.", ResultErrorType.BadRequest);
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

            // 5. Return Result
            var dto = new SetMenuDto(
                setMenu.SetMenuId,
                setMenu.Code,
                setMenu.Name,
                setMenu.Price,
                setMenu.IsOutOfStock,
                setMenu.CreatedAt,
                setMenu.UpdatedAt ?? DateTime.MinValue // Handle null
            );

            return Result<SetMenuDto>.Success(dto);
        }
    }
}
