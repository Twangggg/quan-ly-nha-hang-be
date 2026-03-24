using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignments
{
    public class GetShiftAssignmentsHandler
        : IRequestHandler<GetShiftAssignmentsQuery, Result<PagedResult<GetShiftAssignmentsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetShiftAssignmentsHandler> _logger;

        public GetShiftAssignmentsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<GetShiftAssignmentsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetShiftAssignmentsResponse>>> Handle(
            GetShiftAssignmentsQuery request,
            CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.ShiftAssignmentList}:{queryJson.GetHashCode()}";

            var cached = await _cacheService.GetAsync<PagedResult<GetShiftAssignmentsResponse>>(cacheKey, cancellationToken);
            if (cached != null) return Result<PagedResult<GetShiftAssignmentsResponse>>.Success(cached);

            var query = _unitOfWork.Repository<ShiftAssignment>().Query()
                .Include(a => a.Employee)
                .Include(a => a.Shift)
                .AsNoTracking();

            // 1. Search (Note or Employee Name)
            var searchableFields = new List<Expression<Func<ShiftAssignment, string?>>>
            {
                u => u.Note,
                u => u.Employee.FullName
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Filters (EmployeeId, ShiftId, AssignedDate)
            // minassigneddate / maxassigneddate will be handled by Project Pattern
            var filterMapping = new Dictionary<string, Expression<Func<ShiftAssignment, object?>>>
            {
                { "employeeid", u => u.EmployeeId },
                { "shiftid", u => u.ShiftId },
                { "assigneddate", u => u.AssignedDate }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // 3. Sorting
            var sortMapping = new Dictionary<string, Expression<Func<ShiftAssignment, object?>>>
            {
                {"assignedDate", u => u.AssignedDate},
                {"startTime", u => u.Shift.StartTime},
                {"createdAt", u => u.CreatedAt}
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, u => u.AssignedDate);

            var pagedResult = await query
                .ProjectTo<GetShiftAssignmentsResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.ShiftAssignments, cancellationToken);
            return Result<PagedResult<GetShiftAssignmentsResponse>>.Success(pagedResult);
        }
    }
}
