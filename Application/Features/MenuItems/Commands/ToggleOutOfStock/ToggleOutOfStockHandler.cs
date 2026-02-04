using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public class UpdateStockStatusHandler : IRequestHandler<ToggleOutOfStockCommand, Result<bool>>
    {
        public UpdateStockStatusHandler()
        {
        }

        public async Task<Result<bool>> Handle(ToggleOutOfStockCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
