using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport
{
    public class GetAttendanceReportHandler : IRequestHandler<GetAttendanceReportQuery, Result<PagedResult<GetAttendanceReportResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetAttendanceReportHandler> _logger;

        public GetAttendanceReportHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper,
            ICacheService cacheService,
            ILogger<GetAttendanceReportHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetAttendanceReportResponse>>> Handle(GetAttendanceReportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start handling GetAttendanceReportQuery with Pagination: {@Pagination}", request.Pagination);

            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.AttendanceReportList}:{queryJson.GetHashCode()}";

            var cached = await _cacheService.GetAsync<PagedResult<GetAttendanceReportResponse>>(cacheKey, cancellationToken);
            if (cached != null) 
            {
                _logger.LogInformation("Returning cached results for GetAttendanceReportQuery");
                return Result<PagedResult<GetAttendanceReportResponse>>.Success(cached);
            }

            var query = _unitOfWork.Repository<Attendance>().Query()
                .Include(a => a.Employee)
                .Include(a => a.ShiftAssignment)
                    .ThenInclude(sa => sa.Shift)
                .AsNoTracking();

            var searchableFields = new List<Expression<Func<Attendance, string?>>>
            {
                a => a.Employee.FullName,
                a => a.Employee.EmployeeCode
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Attendance, object?>>>
            {
                { "employeeid", a => a.EmployeeId },
                { "islate", a => a.isLate },
                { "isearlyleave", a => a.isEarlyLeave },
                { "date", a => a.CheckInTime.Date }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Attendance, object?>>>
            {
                { "date", a => a.CheckInTime.Date },
                { "employeename", a => a.Employee.FullName },
                { "checkintime", a => a.CheckInTime },
                { "checkouttime", a => a.CheckOutTime }
            };

            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, a => a.CheckInTime);

            var pagedResult = await query
                .ProjectTo<GetAttendanceReportResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination, cancellationToken);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Attendances, cancellationToken);
            
            _logger.LogInformation("Successfully processed GetAttendanceReportQuery, found {Count} items", pagedResult.Items.Count);
            
            return Result<PagedResult<GetAttendanceReportResponse>>.Success(pagedResult);
        }
    }
}
