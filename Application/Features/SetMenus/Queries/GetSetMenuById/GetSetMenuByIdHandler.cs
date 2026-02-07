using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdHandler : IRequestHandler<GetSetMenuByIdQuery, Result<GetSetMenuByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetSetMenuByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<GetSetMenuByIdResponse>> Handle(GetSetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();
            var setMenuItemRepository = _unitOfWork.Repository<SetMenuItem>();

            // Get existing SetMenu
            var setMenu = await setMenuRepository.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<GetSetMenuByIdResponse>.Failure($"Set Menu with ID '{request.SetMenuId}' not found.", ResultErrorType.NotFound);
            }

            // Get SetMenuItems
            var menuItemById = await setMenuItemRepository.Query()
                .Where(x => x.SetMenuId == request.SetMenuId).ToListAsync(cancellationToken);
            var response = new GetSetMenuByIdResponse
            {
                SetMenuId = setMenu.SetMenuId,
                Code = setMenu.Code,
                Name = setMenu.Name,
                SetType = setMenu.SetType,
                ImageUrl = setMenu.ImageUrl,
                Description = setMenu.Description,
                CostPrice = setMenu.CostPrice,
                Price = setMenu.Price,
                UpdatedAt = setMenu.UpdatedAt,
                UpdatedByEmployeeId = setMenu.UpdatedByEmployeeId,
                Items = menuItemById.Select(item => new GetSetMenuItemByIdResponse
                {
                    SetMenuItemId = item.SetMenuItemId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    CreatedAt = item.CreatedAt
                }).ToList()
            };

            return Result<GetSetMenuByIdResponse>.Success(response);
        }
    }
}
