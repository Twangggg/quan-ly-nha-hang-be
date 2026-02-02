using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public class UpdateStockStatusHandler : IRequestHandler<UpdateStockStatusCommand, Result<bool>>
    {
        public UpdateStockStatusHandler()
        {
        }

        public async Task<Result<bool>> Handle(UpdateStockStatusCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
