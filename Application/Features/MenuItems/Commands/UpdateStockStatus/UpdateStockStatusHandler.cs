using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public class UpdateStockStatusHandler : IRequestHandler<UpdateStockStatusCommand, Result<bool>>
    {
        private readonly IApplicationDbContext _context;

        public UpdateStockStatusHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(UpdateStockStatusCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement UpdateStockStatus logic
            throw new NotImplementedException();
        }
    }
}
