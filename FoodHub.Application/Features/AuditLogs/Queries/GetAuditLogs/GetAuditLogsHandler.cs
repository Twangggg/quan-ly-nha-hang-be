using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
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

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var response = items.Select(log => new GetAuditLogsResponse
            {
                LogId = log.LogId,
                CreatedAt = log.CreatedAt,
                Action = log.Action.ToString(),
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Summary = GenerateSummary(log),
                ActorInfo = log.ActorInfo,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
            }).ToList();

            var pagedResult = new PagedResult<GetAuditLogsResponse>(
                response,
                request,
                totalCount
            );

            return Result<PagedResult<GetAuditLogsResponse>>.Success(pagedResult);
        }

        private static string GenerateSummary(AuditLog log)
        {
            var actionText = log.Action switch
            {
                AuditAction.Create => "Created",
                AuditAction.Update => "Updated",
                AuditAction.Delete => "Deleted",
                AuditAction.StatusChange => "Changed status",
                AuditAction.Login => "Logged in",
                AuditAction.Logout => "Logged out",
                _ => log.Action.ToString()
            };

            return $"{actionText} {log.EntityName}";
        }
    }
}
