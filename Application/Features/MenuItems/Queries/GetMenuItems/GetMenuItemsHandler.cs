using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsHandler : IRequestHandler<GetMenuItemsQuery, Result<PagedResult<GetMenuItemsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMenuItemsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<GetMenuItemsResponse>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<MenuItem>().Query();

            // 1. Apply Global Search
            var searchableFields = new List<Expression<Func<MenuItem, string?>>>
            {
                m => m.Code,
                m => m.Name,
                m => m.Description
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<MenuItem, object?>>>
            {
                { "categoryId", m => m.CategoryId },
                { "station", m => m.Station },
                { "isOutOfStock", m => m.IsOutOfStock }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // Apply Price Range Filter
            if (request.Pagination.Filters != null)
            {
                foreach (var filter in request.Pagination.Filters)
                {
                    var parts = filter.Split(':');
                    if (parts.Length < 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (key.Equals("minPrice", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(value, out var minPrice))
                    {
                        query = query.Where(m => m.PriceDineIn >= minPrice);
                    }
                    else if (key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(value, out var maxPrice))
                    {
                        query = query.Where(m => m.PriceDineIn <= maxPrice);
                    }
                }
            }

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<MenuItem, object?>>>
            {
                { "code", m => m.Code },
                { "name", m => m.Name },
                { "priceDineIn", m => m.PriceDineIn },
                { "priceTakeAway", m => m.PriceTakeAway },
                { "createdAt", m => m.CreatedAt }
            };

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                sortMapping,
                m => m.Name);

            var pagedResult = await query
                .ProjectTo<GetMenuItemsResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<GetMenuItemsResponse>>.Success(pagedResult);
        }
    }
}
