using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.AuditLogs.Queries.GetAuditLogs
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

        public async Task<Result<PagedResult<GetAuditLogsResponse>>> Handle(
            GetAuditLogsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<AuditLog>().Query().AsNoTracking();

            if (request.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(x => x.CreatedAt <= request.ToDate.Value);

            if (!string.IsNullOrEmpty(request.ActionFilter) && Enum.TryParse<AuditAction>(request.ActionFilter, true, out var actionEnum))
                query = query.Where(x => x.Action == actionEnum);

            if (!string.IsNullOrEmpty(request.EntityNameFilter))
                query = query.Where(x => x.EntityName == request.EntityNameFilter);

            if (!string.IsNullOrEmpty(request.EntityIdFilter))
                query = query.Where(x => x.EntityId.Contains(request.EntityIdFilter));

            var pagedResult = await query
                .OrderByDescending(x => x.CreatedAt)
                .ProjectTo<GetAuditLogsResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

            return Result<PagedResult<GetAuditLogsResponse>>.Success(pagedResult);
        }
    }
}
