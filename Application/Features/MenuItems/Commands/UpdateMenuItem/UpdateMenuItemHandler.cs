using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Resources;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemHandler : IRequestHandler<UpdateMenuItemCommand, Result<UpdateMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ErrorMessages> _localizer;

        public UpdateMenuItemHandler(IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        public async Task<Result<UpdateMenuItemResponse>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.Repository<MenuItem>().GetByIdAsync(request.MenuItemId);

            if (menuItem == null)
            {
                return Result<UpdateMenuItemResponse>.Failure(_localizer["MenuItem.NotFound", request.MenuItemId].Value);
            }

            // Update fields
            menuItem.Code = request.Code;
            menuItem.Name = request.Name;
            menuItem.ImageUrl = request.ImageUrl;
            menuItem.Description = request.Description;
            menuItem.CategoryId = request.CategoryId;
            menuItem.Station = request.Station;
            menuItem.ExpectedTime = request.ExpectedTime;
            menuItem.PriceDineIn = request.PriceDineIn;
            menuItem.PriceTakeAway = request.PriceTakeAway ?? request.PriceDineIn;
            menuItem.Cost = request.Cost ?? 0;
            menuItem.IsOutOfStock = request.IsOutOfStock;
            
            // Check if name changed and new name exists? (Optional, skipping for now as not required)

            _unitOfWork.Repository<MenuItem>().Update(menuItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new UpdateMenuItemResponse
            {
                MenuItemId = menuItem.Id,
                Code = menuItem.Code,
                Name = menuItem.Name,
                ImageUrl = menuItem.ImageUrl,
                Description = menuItem.Description,
                CategoryId = menuItem.CategoryId,
                Station = (int)menuItem.Station,
                ExpectedTime = menuItem.ExpectedTime,
                PriceDineIn = menuItem.PriceDineIn,
                PriceTakeAway = menuItem.PriceTakeAway,
                Cost = menuItem.Cost,
                IsOutOfStock = menuItem.IsOutOfStock
            };

            return Result<UpdateMenuItemResponse>.Success(response);
        }
    }
}
