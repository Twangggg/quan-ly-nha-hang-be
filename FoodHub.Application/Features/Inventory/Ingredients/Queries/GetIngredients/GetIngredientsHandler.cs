using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients
{
    public class GetIngredientsHandler
        : IRequestHandler<GetIngredientsQuery, Result<PagedResult<GetIngredientsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetIngredientsHandler> _logger;

        public GetIngredientsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetIngredientsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetIngredientsResponse>>> Handle(
            GetIngredientsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetIngredients with search {Search}",
                request.Pagination.Search
            );

            try
            {
                var query = _unitOfWork.Repository<Ingredient>().Query().AsNoTracking();

                // 1. Global Search
                var searchableFields = new List<Expression<Func<Ingredient, string?>>>
                {
                    x => x.Name,
                    x => x.Code,
                };
                query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

                // 2. Filters
                var filterMapping = new Dictionary<string, Expression<Func<Ingredient, object?>>>
                {
                    { "isActive", x => x.IsActive },
                    { "unit", x => x.BaseUnit },
                };
                query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

                // 3. Sorting
                var sortMapping = new Dictionary<string, Expression<Func<Ingredient, object?>>>
                {
                    { "name", x => x.Name },
                    { "code", x => x.Code },
                    { "currentStock", x => x.CurrentStock },
                    { "createdAt", x => x.CreatedAt },
                };

                query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, x => x.Name);

                var pagedResult = await query
                    .ProjectTo<GetIngredientsResponse>(_mapper.ConfigurationProvider)
                    .ToPagedResultAsync(request.Pagination);

                _logger.LogInformation(
                    "End handling GetIngredients with {Count} items",
                    pagedResult.Items.Count
                );
                return Result<PagedResult<GetIngredientsResponse>>.Success(pagedResult);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while getting ingredients with search {Search}",
                    request.Pagination.Search
                );
                throw;
            }
        }
    }
}
