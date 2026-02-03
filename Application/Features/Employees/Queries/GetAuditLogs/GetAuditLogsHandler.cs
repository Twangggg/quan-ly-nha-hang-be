using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using FoodHub.Application.Extensions.Pagination;
using System.Linq.Expressions;

namespace FoodHub.Application.Features.Employees.Queries.GetAuditLogs
{
    public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<GetAuditLogsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAuditLogsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<GetAuditLogsResponse>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<AuditLog>().Query()
                .Where(x => x.TargetId == request.EmployeeId);

            var filterMapping = new Dictionary<string, Expression<Func<AuditLog, object?>>>
            {
                { "action", x => x.Action.ToString() }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                new Dictionary<string, Expression<Func<AuditLog, object?>>>
                {
                    { "action", x => x.Action },
                    { "time", x => x.CreatedAt },
                    { "actor", x => x.PerformedBy.FullName }
                },
                x => x.LogId
            );

            if (string.IsNullOrWhiteSpace(request.Pagination.OrderBy))
            {
                query = query.OrderByDescending(x => x.CreatedAt);
            }

            var pagedResult = await query
                .ProjectTo<GetAuditLogsResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<GetAuditLogsResponse>>.Success(pagedResult);
        }
    }
}
