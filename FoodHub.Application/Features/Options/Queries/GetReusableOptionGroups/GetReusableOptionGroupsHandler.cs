using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Queries.GetReusableOptionGroups
{
    public class GetReusableOptionGroupsHandler
        : IRequestHandler<GetReusableOptionGroupsQuery, Result<PagedResult<OptionGroupResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetReusableOptionGroupsHandler> _logger;

        public GetReusableOptionGroupsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetReusableOptionGroupsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<OptionGroupResponse>>> Handle(
            GetReusableOptionGroupsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start querying reusable option groups");
            var cacheKey = string.Format(
                CacheKey.OptionReusableList,
                request.PageNumber,
                request.PageSize
            );

            var cached = await _cacheService.GetAsync<PagedResult<OptionGroupResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End querying reusable option groups PageNumber={PageNumber} TotalCount={TotalCount} (from cache)",
                    request.PageNumber,
                    cached.TotalCount
                );

                return Result<PagedResult<OptionGroupResponse>>.Success(cached);
            }

            var baseQuery = _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .AsNoTracking()
                .Where(og => og.MenuItemId == null) // REUSABLE
                .OrderBy(og => og.Name);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var optionGroups = await baseQuery
                .Include(og => og.OptionItems)
                .Include(og => og.MenuItemOptionGroups)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mappedGroups = optionGroups
                .Select(og => new OptionGroupResponse
                {
                    OptionGroupId = og.OptionGroupId,
                    Name = og.Name,
                    Type = (int)og.OptionType,
                    IsRequired = og.IsRequired,
                    IsVisible = true,
                    UsageCount = og.MenuItemOptionGroups.Count,
                    CreatedAt = og.CreatedAt,
                    UpdatedAt = og.UpdatedAt,
                    OptionItems = og
                        .OptionItems.OrderBy(oi => oi.Label)
                        .Select(oi => new OptionItemResponse
                        {
                            OptionItemId = oi.OptionItemId,
                            OptionGroupId = oi.OptionGroupId,
                            Label = oi.Label,
                            ExtraPrice = oi.ExtraPrice,
                        })
                        .ToList(),
                })
                .ToList();

            var pagedGroups = new PagedResult<OptionGroupResponse>(
                mappedGroups,
                new PaginationParams
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                },
                totalCount
            );

            _logger.LogInformation(
                "End querying reusable option groups PageNumber={PageNumber} TotalCount={TotalCount}",
                request.PageNumber,
                pagedGroups.TotalCount
            );

            await _cacheService.SetAsync(
                cacheKey,
                pagedGroups,
                CacheTTL.Options,
                cancellationToken
            );

            return Result<PagedResult<OptionGroupResponse>>.Success(pagedGroups);
        }
    }
}
