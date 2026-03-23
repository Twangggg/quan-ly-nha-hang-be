using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Costing.Commands.RecalculateCogs
{
    /// <summary>
    /// Recalculate weighted-average COGS for stock-out receipts within a period.
    /// </summary>
    public class RecalculateCogsCommand : IRequest<Result<RecalculateCogsResponse>>
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public Guid? IngredientId { get; set; }
    }
}
