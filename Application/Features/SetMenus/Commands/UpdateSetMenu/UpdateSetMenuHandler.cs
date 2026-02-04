using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuHandler : IRequestHandler<UpdateSetMenuCommand, Result<UpdateSetMenuResponse>>
    {
        public UpdateSetMenuHandler()
        {
        }

        public async Task<Result<UpdateSetMenuResponse>> Handle(UpdateSetMenuCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
