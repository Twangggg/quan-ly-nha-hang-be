using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusHandler : IRequestHandler<GetSetMenusQuery, Result<PagedResult<GetSetMenusResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetSetMenusHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<GetSetMenusResponse>>> Handle(GetSetMenusQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<SetMenu>().Query();

            // 1. Apply Global Search (search by Code, Name, Description)
            var searchableFields = new List<Expression<Func<SetMenu, string?>>>
            {
                s => s.Code,
                s => s.Name,
                s => s.Description
            };
            query = query.ApplyGlobalSearch(request.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<SetMenu, object?>>>
            {
                { "isOutOfStock", s => s.IsOutOfStock }
            };
            query = query.ApplyFilters(request.Filters, filterMapping);

            // Apply Price Range Filter
            if (request.Filters != null)
            {
                foreach (var filter in request.Filters)
                {
                    var parts = filter.Split(':');
                    if (parts.Length < 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (key.Equals("minPrice", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(value, out var minPrice))
                    {
                        query = query.Where(s => s.Price >= minPrice);
                    }
                    else if (key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(value, out var maxPrice))
                    {
                        query = query.Where(s => s.Price <= maxPrice);
                    }
                }
            }

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<SetMenu, object?>>>
            {
                { "code", s => s.Code },
                { "name", s => s.Name },
                { "price", s => s.Price },
                { "createdAt", s => s.CreatedAt }
            };

            query = query.ApplySorting(
                request.OrderBy,
                sortMapping,
                s => s.Name);

            var pagedResult = await query
                .ProjectTo<GetSetMenusResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.PageNumber, request.PageSize);

            return Result<PagedResult<GetSetMenusResponse>>.Success(pagedResult);
        }
    }
}
