using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftsByEmployeeId
{
    public class GetSAsByEmployeeIdHandler : IRequestHandler<GetSAsByEmployeeIdQuery, Result<PagedResult<GetSAsByEmployeeIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetSAsByEmployeeIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<GetSAsByEmployeeIdResponse>>> Handle(GetSAsByEmployeeIdQuery request, CancellationToken cancellationToken)
        {
            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();

            var shiftAssignmentRepository = _unitOfWork.Repository<ShiftAssignment>();

            var query = shiftAssignmentRepository
                .Query()
                .Include(s => s.Shift)
                .Where(sa => sa.EmployeeId == auditorId)
                .AsNoTracking();

            // 1. Search (Note or Employee Name)
            var searchableFields = new List<Expression<Func<ShiftAssignment, string?>>>
            {
                u => u.Note
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Filters (EmployeeId, ShiftId, AssignedDate)
            // minassigneddate / maxassigneddate will be handled by Project Pattern
            var filterMapping = new Dictionary<string, Expression<Func<ShiftAssignment, object?>>>
            {
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
                .ProjectTo<GetSAsByEmployeeIdResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<GetSAsByEmployeeIdResponse>>.Success(pagedResult);
        }
    }
}
