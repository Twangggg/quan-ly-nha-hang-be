using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.Groups.Queries.GetInventoryGroups
{
    public sealed class GetInventoryGroupsHandler
        : IRequestHandler<GetInventoryGroupsQuery, Result<List<GetInventoryGroupsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetInventoryGroupsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<GetInventoryGroupsResponse>>> Handle(
            GetInventoryGroupsQuery request,
            CancellationToken cancellationToken
        )
        {
            var groups = await _unitOfWork
                .Repository<InventoryGroup>()
                .Query()
                .AsNoTracking()
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Name)
                .Select(x => new GetInventoryGroupsResponse
                {
                    InventoryGroupId = x.InventoryGroupId,
                    Name = x.Name,
                    Description = x.Description,
                    LowStockThreshold = x.LowStockThreshold,
                    ExpiryWarningDays = x.ExpiryWarningDays,
                    DefaultCostMethod = x.DefaultCostMethod,
                    IngredientCount = x.Ingredients.Count,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return Result<List<GetInventoryGroupsResponse>>.Success(groups);
        }
    }
}
