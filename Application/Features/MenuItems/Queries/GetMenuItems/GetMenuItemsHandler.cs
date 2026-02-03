using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsHandler : IRequestHandler<GetMenuItemsQuery, Result<PagedResult<MenuItemDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMenuItemsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<MenuItemDto>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<MenuItem>().Query()
                .Include(x => x.Category)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(request.SearchCode))
            {
                query = query.Where(x => x.Code.Contains(request.SearchCode) || x.Name.Contains(request.SearchCode));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId.Value);
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(x => x.PriceDineIn >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(x => x.PriceDineIn <= request.MaxPrice.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var canViewCost = _currentUserService.IsInRole("Manager") || _currentUserService.IsInRole("Cashier");

            var dtos = items.Select(m => new MenuItemDto
            {
                MenuItemId = m.MenuItemId,
                Code = m.Code,
                Name = m.Name,
                ImageUrl = m.ImageUrl,
                Description = m.Description,
                CategoryId = m.CategoryId,
                CategoryName = m.Category?.Name ?? string.Empty,
                Station = (int)m.Station,
                ExpectedTime = m.ExpectedTime,
                PriceDineIn = m.PriceDineIn,
                PriceTakeAway = m.PriceTakeAway,
                Cost = canViewCost ? m.Cost : null,
                IsOutOfStock = m.IsOutOfStock,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();

            var paginationParams = new PaginationParams
            {
                PageIndex = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = new PagedResult<MenuItemDto>(dtos, paginationParams, totalCount);

            return Result<PagedResult<MenuItemDto>>.Success(result);
        }
    }
}
