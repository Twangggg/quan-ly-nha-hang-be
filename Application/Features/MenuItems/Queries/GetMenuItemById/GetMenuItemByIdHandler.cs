using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Constants;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public class GetMenuItemByIdHandler : IRequestHandler<GetMenuItemByIdQuery, Result<GetMenuItemByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public GetMenuItemByIdHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<GetMenuItemByIdResponse>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .Include(m => m.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuItemId == request.Id, cancellationToken);

            if (menuItem == null)
            {
                return Result<GetMenuItemByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound, request.Id));
            }

            var response = new GetMenuItemByIdResponse
            {
                MenuItemId = menuItem.MenuItemId,
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
                Cost = menuItem.CostPrice,
                IsOutOfStock = menuItem.IsOutOfStock,
                CreatedAt = menuItem.CreatedAt,
                UpdatedAt = menuItem.UpdatedAt ?? menuItem.CreatedAt
            };


            return Result<GetMenuItemByIdResponse>.Success(response);
        }
    }
}

