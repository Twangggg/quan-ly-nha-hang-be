using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public class UpdateSetMenuStockStatusHandler : IRequestHandler<UpdateSetMenuStockStatusCommand, Result<bool>>
    {
        public UpdateSetMenuStockStatusHandler()
        {
        }

        public async Task<Result<bool>> Handle(UpdateSetMenuStockStatusCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
