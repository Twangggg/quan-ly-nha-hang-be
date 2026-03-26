using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace FoodHub.Application.Features.Shifts.Queries.GetShifts
{
    public class GetShiftsHandler : IRequestHandler<GetShiftsQuery, Result<PagedResult<GetShiftByIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetShiftsHandler> _logger;

        public GetShiftsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<GetShiftsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetShiftByIdResponse>>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.ShiftList}:{queryJson.GetHashCode()}";

            var cached = await _cacheService.GetAsync<PagedResult<GetShiftByIdResponse>>(cacheKey, cancellationToken);
            if (cached != null) return Result<PagedResult<GetShiftByIdResponse>>.Success(cached);

            var query = _unitOfWork.Repository<Shift>().Query().AsNoTracking();

            // 1. Search (ByName)
            var searchableFields = new List<Expression<Func<Shift, string?>>> { s => s.Name };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Filters (Status)
            var filterMapping = new Dictionary<string, Expression<Func<Shift, object?>>> { { "status", s => s.Status } };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // 3. Sorting
            var sortMapping = new Dictionary<string, Expression<Func<Shift, object?>>>
            {
                {"name", s => s.Name},
                {"startTime", s => s.StartTime},
                {"endTime", s => s.EndTime},
                {"createdAt", s => s.CreatedAt}
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, s => s.ShiftId);

            var pagedResult = await query
                .ProjectTo<GetShiftByIdResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Shifts, cancellationToken);
            return Result<PagedResult<GetShiftByIdResponse>>.Success(pagedResult);
        }
    }
}
