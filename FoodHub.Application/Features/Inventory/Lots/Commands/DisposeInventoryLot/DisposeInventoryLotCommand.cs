using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Lots.Commands.DisposeInventoryLot
{
    public class DisposeInventoryLotCommand : IRequest<Result<DisposeInventoryLotResponse>>
    {
        public Guid LotId { get; set; }
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
