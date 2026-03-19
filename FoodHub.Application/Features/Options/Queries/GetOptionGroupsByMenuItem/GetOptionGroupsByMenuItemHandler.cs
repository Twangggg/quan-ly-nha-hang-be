using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
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
        private readonly ILogger<GetOptionGroupsByMenuItemHandler> _logger;

        public GetOptionGroupsByMenuItemHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetOptionGroupsByMenuItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
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

            var groups = await _unitOfWork
                .Repository<MenuItemOptionGroup>()
                .Query()
                .AsNoTracking()
                .Where(miog => miog.MenuItemId == request.MenuItemId && miog.IsVisible)
                .OrderBy(miog => miog.SortOrder)
                .ThenBy(miog => miog.OptionGroup.Name)
                .Select(miog => new OptionGroupResponse
                {
                    MenuItemOptionGroupId = miog.MenuItemOptionGroupId,
                    OptionGroupId = miog.OptionGroupId,
                    MenuItemId = miog.MenuItemId,
                    Name = miog.OptionGroup.Name,
                    Type = (int)miog.OptionGroup.OptionType,
                    IsRequired = miog.IsRequired,
                    MinSelect = miog.MinSelect,
                    MaxSelect = miog.MaxSelect,
                    SortOrder = miog.SortOrder,
                    IsVisible = miog.IsVisible,
                    OptionItems = miog.OptionGroup.OptionItems
                        .OrderBy(oi => oi.Label)
                        .Select(oi => new OptionItemResponse
                        {
                            OptionItemId = oi.OptionItemId,
                            OptionGroupId = oi.OptionGroupId,
                            Label = oi.Label,
                            ExtraPrice = oi.ExtraPrice,
                        })
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "End querying option groups for MenuItemId={MenuItemId} Count={OptionGroupCount}",
                request.MenuItemId,
                groups.Count
            );

            return Result<List<OptionGroupResponse>>.Success(groups);
        }
    }
}
