using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemHandler : IRequestHandler<UpdateMenuItemCommand, Result<UpdateMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public UpdateMenuItemHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<UpdateMenuItemResponse>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<MenuItem>();

            var menuItem = await repo.Query()
                .Include(mi => mi.Category)
                .FirstOrDefaultAsync(mi => mi.MenuItemId == request.MenuItemId, cancellationToken);
            if (menuItem is null) return Result<UpdateMenuItemResponse>.NotFound(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));

            var name = request.Name.Trim();
            var imageUrl = request.ImageUrl.Trim();
            var description = request.Description?.Trim();
            var categoryId = request.CategoryId;
            var station = request.Station;
            var expectedTime = request.ExpectedTime;
            var priceDineIn = request.PriceDineIn;
            var priceTakeAway = request.PriceTakeAway;
            var costPrice = request.CostPrice;

            var categoryExists = await _unitOfWork.Repository<Category>().Query()
                .AnyAsync(c => c.CategoryId == categoryId, cancellationToken);
            if (!categoryExists)
                return Result<UpdateMenuItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Category.NotFound));

            menuItem.Name = name;
            menuItem.ImageUrl = imageUrl;
            menuItem.Description = description;
            menuItem.CategoryId = categoryId;
            menuItem.Station = station;
            menuItem.ExpectedTime = expectedTime;
            menuItem.PriceDineIn = priceDineIn;
            menuItem.PriceTakeAway = priceTakeAway;

            menuItem.UpdatedAt = DateTime.UtcNow;
            menuItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            if (costPrice.HasValue)
            {
                if (_currentUserService.Role is EmployeeRole.Manager or EmployeeRole.Cashier)
                    menuItem.CostPrice = costPrice.Value;
                else return Result<UpdateMenuItemResponse>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.UpdateCostForbidden), ResultErrorType.Forbidden);
            }

            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveByPatternAsync("menuitem:list", cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.MenuItemById, request.MenuItemId), cancellationToken);

            var response = _mapper.Map<UpdateMenuItemResponse>(menuItem);
            return Result<UpdateMenuItemResponse>.Success(response);
        }
    }
}
