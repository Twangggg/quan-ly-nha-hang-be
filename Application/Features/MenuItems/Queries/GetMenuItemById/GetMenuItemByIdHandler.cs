using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public class GetMenuItemByIdHandler : IRequestHandler<GetMenuItemByIdQuery, Result<MenuItemDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetMenuItemByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<MenuItemDetailDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .Include(m => m.Category)
                .Include(m => m.OptionGroups)
                    .ThenInclude(og => og.OptionItems)
                .FirstOrDefaultAsync(m => m.MenuItemId == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<MenuItemDetailDto>.Failure($"Menu item with ID {request.MenuItemId} not found.", ResultErrorType.NotFound);
            }

            var dto = new MenuItemDetailDto
            {
                MenuItemId = menuItem.MenuItemId,
                Code = menuItem.Code,
                Name = menuItem.Name,
                ImageUrl = menuItem.ImageUrl,
                Description = menuItem.Description,
                CategoryId = menuItem.CategoryId,
                CategoryName = menuItem.Category?.Name ?? string.Empty,
                Station = (int)menuItem.Station,
                ExpectedTime = menuItem.ExpectedTime,
                PriceDineIn = menuItem.PriceDineIn,
                PriceTakeAway = menuItem.PriceTakeAway,
                Cost = menuItem.Cost,
                IsOutOfStock = menuItem.IsOutOfStock,
                CreatedAt = menuItem.CreatedAt,
                UpdatedAt = menuItem.UpdatedAt ?? DateTime.MinValue,
                OptionGroups = menuItem.OptionGroups.Select(og => new OptionGroupDto
                {
                    OptionGroupId = og.OptionGroupId,
                    Name = og.Name,
                    Type = (int)og.Type,
                    IsRequired = og.IsRequired,
                    OptionItems = og.OptionItems.Select(oi => new OptionItemDto
                    {
                        OptionItemId = oi.OptionItemId,
                        Label = oi.Label,
                        ExtraPrice = oi.ExtraPrice
                    }).ToList()
                }).ToList()
            };

            return Result<MenuItemDetailDto>.Success(dto);
        }
    }
}
