using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Resources;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public class GetMenuItemByIdHandler : IRequestHandler<GetMenuItemByIdQuery, Result<MenuItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ErrorMessages> _localizer;

        public GetMenuItemByIdHandler(IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        public async Task<Result<MenuItemDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .Include(m => m.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (menuItem == null)
            {
                return Result<MenuItemDto>.Failure(_localizer["MenuItem.NotFound", request.Id].Value);
            }

            var dto = new MenuItemDto
            {
                MenuItemId = menuItem.Id,
                Code = menuItem.Code,
                Name = menuItem.Name,
                ImageUrl = menuItem.ImageUrl,
                Description = menuItem.Description,
                CategoryId = menuItem.CategoryId,
                CategoryName = menuItem.Category.Name,
                Station = (int)menuItem.Station,
                ExpectedTime = menuItem.ExpectedTime,
                PriceDineIn = menuItem.PriceDineIn,
                PriceTakeAway = menuItem.PriceTakeAway,
                Cost = menuItem.Cost,
                IsOutOfStock = menuItem.IsOutOfStock,
                CreatedAt = menuItem.CreatedAt,
                UpdatedAt = menuItem.UpdatedAt ?? menuItem.CreatedAt
            };

            return Result<MenuItemDto>.Success(dto);
        }
    }
}
