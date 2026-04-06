using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdHandler : IRequestHandler<GetSetMenuByIdQuery, Result<GetSetMenuByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public GetSetMenuByIdHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<GetSetMenuByIdResponse>> Handle(GetSetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.SetMenuById, request.SetMenuId);

            var cachedSetMenu = await _cacheService.GetAsync<GetSetMenuByIdResponse>(
                cacheKey,
                cancellationToken);
            if (cachedSetMenu != null)
            {
                return Result<GetSetMenuByIdResponse>.Success(cachedSetMenu);
            }

            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();
            var setMenuItemRepository = _unitOfWork.Repository<SetMenuItem>();

            // Get existing SetMenu
            var setMenu = await setMenuRepository.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<GetSetMenuByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.SetMenu.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // Get SetMenuItems
            var menuItemById = await setMenuItemRepository.Query()
                .Where(x => x.SetMenuId == request.SetMenuId).ToListAsync(cancellationToken);
            var response = new GetSetMenuByIdResponse
            {
                SetMenuId = setMenu.SetMenuId,
                Code = setMenu.Code,
                Name = setMenu.Name,
                ImageUrl = setMenu.ImageUrl,
                Description = setMenu.Description,
                CostPrice = setMenu.CostPrice,
                Price = setMenu.Price,
                UpdatedAt = setMenu.UpdatedAt,
                UpdatedByEmployeeId = setMenu.UpdatedBy,
                Items = menuItemById.Select(item => new GetSetMenuItemByIdResponse
                {
                    SetMenuItemId = item.SetMenuItemId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    CreatedAt = item.CreatedAt
                }).ToList()
            };

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.SetMenus, cancellationToken);
            return Result<GetSetMenuByIdResponse>.Success(response);
        }
    }
}
