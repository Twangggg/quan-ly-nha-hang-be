using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Resources;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemHandler : IRequestHandler<CreateMenuItemCommand, Result<CreateMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ErrorMessages> _localizer;

        public CreateMenuItemHandler(IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        public async Task<Result<CreateMenuItemResponse>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var menuItem = new MenuItem
            {
                Code = request.Code,
                Name = request.Name,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                CategoryId = request.CategoryId,
                Station = (Station)request.Station,
                ExpectedTime = request.ExpectedTime ?? 0,
                PriceDineIn = request.PriceDineIn,
                PriceTakeAway = request.PriceTakeAway ?? request.PriceDineIn,
                Cost = request.Cost ?? 0
            };

            await _unitOfWork.Repository<MenuItem>().AddAsync(menuItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new CreateMenuItemResponse
            {
                MenuItemId = menuItem.Id
            };

            return Result<CreateMenuItemResponse>.Success(response);
        }
    }
}
