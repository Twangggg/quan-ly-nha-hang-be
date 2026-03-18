using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm
{
    public class GetInventoryCheckCreateFormHandler
        : IRequestHandler<
            GetInventoryCheckCreateFormQuery,
            Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>
        >
    {
        private readonly ILogger<GetInventoryCheckCreateFormHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public GetInventoryCheckCreateFormHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetInventoryCheckCreateFormHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>> Handle(
            GetInventoryCheckCreateFormQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetInventoryCheckCreateForm");

            var items = await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new GetInventoryCheckCreateFormResponse
                {
                    IngredientId = x.IngredientId,
                    IngredientCode = x.Code,
                    IngredientName = x.Name,
                    BaseUnit = x.BaseUnit,
                    BookQuantity = x.CurrentStock,
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "End handling GetInventoryCheckCreateForm with {Count} items",
                items.Count
            );

            return Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>.Success(items);
        }
    }
}
