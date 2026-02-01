using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public class UpdateSetMenuStockStatusHandler : IRequestHandler<UpdateSetMenuStockStatusCommand, Result<bool>>
    {
        private readonly IApplicationDbContext _context;

        public UpdateSetMenuStockStatusHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(UpdateSetMenuStockStatusCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement UpdateSetMenuStockStatus logic
            throw new NotImplementedException();
        }
    }
}
