using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsByMenuItemHandler
        : IRequestHandler<GetOptionGroupsByMenuItemQuery, Result<List<OptionGroupResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetOptionGroupsByMenuItemHandler> _logger;

        public GetOptionGroupsByMenuItemHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetOptionGroupsByMenuItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<List<OptionGroupResponse>>> Handle(
            GetOptionGroupsByMenuItemQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start querying option groups for MenuItemId={MenuItemId}",
                request.MenuItemId
            );

            var cacheKey = string.Format(CacheKey.OptionGroupsByMenuItem, request.MenuItemId);
            var cached = await _cacheService.GetAsync<List<OptionGroupResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End querying option groups for MenuItemId={MenuItemId} Count={OptionGroupCount} (from cache)",
                    request.MenuItemId,
                    cached.Count
                );

                return Result<List<OptionGroupResponse>>.Success(cached);
            }

            var assignmentRows = await _unitOfWork
                .Repository<MenuItemOptionGroup>()
                .Query()
                .AsNoTracking()
                .Where(miog => miog.MenuItemId == request.MenuItemId && miog.IsVisible)
                .OrderBy(miog => miog.SortOrder)
                .ThenBy(miog => miog.OptionGroup.Name)
                .Select(miog => new
                {
                    miog.MenuItemOptionGroupId,
                    miog.OptionGroupId,
                    miog.MenuItemId,
                    GroupName = miog.OptionGroup.Name,
                    OptionType = miog.OptionGroup.OptionType,
                    miog.IsRequired,
                    miog.MinSelect,
                    miog.MaxSelect,
                    miog.SortOrder,
                    miog.IsVisible,
                    miog.OptionGroup.CreatedAt,
                    miog.OptionGroup.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            var optionGroupIds = assignmentRows.Select(x => x.OptionGroupId).Distinct().ToList();

            var optionItems = await _unitOfWork
                .Repository<OptionItem>()
                .Query()
                .AsNoTracking()
                .Where(oi => optionGroupIds.Contains(oi.OptionGroupId))
                .OrderBy(oi => oi.Label)
                .Select(oi => new OptionItemResponse
                {
                    OptionItemId = oi.OptionItemId,
                    OptionGroupId = oi.OptionGroupId,
                    Label = oi.Label,
                    ExtraPrice = oi.ExtraPrice,
                })
                .ToListAsync(cancellationToken);

            var optionItemsLookup = optionItems
                .GroupBy(oi => oi.OptionGroupId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var mappedGroups = assignmentRows
                .Select(miog => new OptionGroupResponse
                {
                    MenuItemOptionGroupId = miog.MenuItemOptionGroupId,
                    OptionGroupId = miog.OptionGroupId,
                    MenuItemId = miog.MenuItemId,
                    Name = miog.GroupName,
                    Type = (int)miog.OptionType,
                    IsRequired = miog.IsRequired,
                    MinSelect = miog.MinSelect,
                    MaxSelect = miog.MaxSelect,
                    SortOrder = miog.SortOrder,
                    IsVisible = miog.IsVisible,
                    CreatedAt = miog.CreatedAt,
                    UpdatedAt = miog.UpdatedAt,
                    OptionItems = optionItemsLookup.TryGetValue(miog.OptionGroupId, out var items)
                        ? items
                        : new List<OptionItemResponse>(),
                })
                .ToList();

            _logger.LogInformation(
                "End querying option groups for MenuItemId={MenuItemId} Count={OptionGroupCount}",
                request.MenuItemId,
                mappedGroups.Count
            );

            await _cacheService.SetAsync(
                cacheKey,
                mappedGroups,
                CacheTTL.Options,
                cancellationToken
            );

            return Result<List<OptionGroupResponse>>.Success(mappedGroups);
        }
    }
}
