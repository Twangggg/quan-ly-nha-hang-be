using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuHandler : IRequestHandler<UpdateSetMenuCommand, Result<UpdateSetMenuResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public UpdateSetMenuHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<UpdateSetMenuResponse>> Handle(UpdateSetMenuCommand request, CancellationToken cancellationToken)
        {
            var setMenuRepository = _unitOfWork.Repository<SetMenu>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();
            var setMenuItemRepository = _unitOfWork.Repository<SetMenuItem>();

            var userRole = _currentUserService.Role;
            if (userRole is not EmployeeRole.Manager)
            {
                return Result<UpdateSetMenuResponse>.Failure(_messageService.GetMessage(MessageKeys.SetMenu.UpdateForbidden), ResultErrorType.Forbidden);
            }

            // 1. Get existing SetMenu
            var setMenu = await setMenuRepository.GetByIdAsync(request.SetMenuId);
            if (setMenu == null)
            {
                return Result<UpdateSetMenuResponse>.Failure(_messageService.GetMessage(MessageKeys.SetMenu.NotFound), ResultErrorType.NotFound);
            }

            // 2. Validate if all MenuItems exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var existingMenuItemsCount = await menuItemRepository.CountAsync(mi => menuItemIds.Contains(mi.MenuItemId));

            if (existingMenuItemsCount != menuItemIds.Count)
            {
                return Result<UpdateSetMenuResponse>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound), ResultErrorType.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 3. Update SetMenu properties
                setMenu.Name = request.Name;
                setMenu.SetType = request.SetType;
                setMenu.Price = request.Price;
                setMenu.ImageUrl = request.ImageUrl;
                setMenu.Description = request.Description;
                setMenu.CostPrice = request.CostPrice;
                setMenu.UpdatedAt = DateTime.UtcNow;
                setMenu.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

                // 4. Handle SetMenuItems
                var existingItems = await setMenuItemRepository.Query()
                    .Where(ei => ei.SetMenuId == request.SetMenuId)
                    .ToListAsync();
                var requestItemIds = request.Items.Select(x => x.MenuItemId).ToList();
                var toRemove = existingItems.Where(x => !requestItemIds.Contains(x.MenuItemId));
                foreach (var item in toRemove)
                    setMenuItemRepository.Delete(item);

                // Add or update items
                foreach (var req in request.Items)
                {
                    var existing = existingItems.FirstOrDefault(x => x.MenuItemId == req.MenuItemId);
                    if (existing != null)
                    {
                        existing.Quantity = req.Quantity;
                    }
                    else
                    {
                        await setMenuItemRepository.AddAsync(new SetMenuItem
                        {
                            SetMenuItemId = Guid.NewGuid(),
                            SetMenuId = request.SetMenuId,
                            MenuItemId = req.MenuItemId,
                            Quantity = req.Quantity,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // 5. Save changes
                setMenuRepository.Update(setMenu);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                // 6. Return Response
                var updatedItems = await setMenuItemRepository.Query()
                    .Where(x => x.SetMenuId == request.SetMenuId)
                    .ToListAsync(cancellationToken);
                var response = new UpdateSetMenuResponse
                {
                    SetMenuId = setMenu.SetMenuId,
                    Code = setMenu.Code,
                    Name = setMenu.Name,
                    SetType = setMenu.SetType,
                    ImageUrl = setMenu.ImageUrl,
                    Description = setMenu.Description,
                    CostPrice = setMenu.CostPrice,
                    Price = setMenu.Price,
                    UpdatedAt = setMenu.UpdatedAt.Value,
                    Items = updatedItems.Select(item => new UpdateSetMenuItemResponse
                    {
                        SetMenuItemId = item.SetMenuItemId,
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        CreatedAt = item.CreatedAt
                    }).ToList()
                };

                return Result<UpdateSetMenuResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<UpdateSetMenuResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError), ResultErrorType.BadRequest);
            }
        }
    }
}
