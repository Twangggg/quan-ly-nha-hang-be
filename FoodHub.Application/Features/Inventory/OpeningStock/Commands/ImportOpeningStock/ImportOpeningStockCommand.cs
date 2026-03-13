using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock
{
    public record ImportOpeningStockCommand(
        List<OpeningStockItemDto> Items,
        bool ConfirmOverwrite
    ) : IRequest<Result<ImportOpeningStockResponse>>;
}
