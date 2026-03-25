using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Features.Promotions.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Promotions.Queries.GetPromotions
{
    public class GetPromotionsHandler
        : IRequestHandler<GetPromotionsQuery, Result<PagedResult<PromotionResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetPromotionsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<PromotionResponse>>> Handle(
            GetPromotionsQuery request,
            CancellationToken cancellationToken
        )
        {
            var query = _unitOfWork
                .Repository<Promotion>()
                .Query()
                .Where(p => p.DeletedAt == null);

            var searchableFields = new List<Expression<Func<Promotion, string?>>>
            {
                p => p.Code,
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Promotion, object?>>>
            {
                { "type", p => p.Type },
                { "isActive", p => p.IsActive },
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Promotion, object?>>>
            {
                { "code", p => p.Code },
                { "type", p => p.Type },
                { "isActive", p => p.IsActive },
                { "startDate", p => p.StartDate },
                { "endDate", p => p.EndDate },
                { "usedCount", p => p.UsedCount },
                { "createdAt", p => p.CreatedAt },
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, p => p.Code);

            var pagedResult = await query
                .ProjectTo<PromotionResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<PromotionResponse>>.Success(pagedResult);
        }
    }
}
