using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuHandler : IRequestHandler<CreateSetMenuCommand, Result<CreateSetMenuResponse>>
    {
        public CreateSetMenuHandler()
        {
        }

        public async Task<Result<CreateSetMenuResponse>> Handle(CreateSetMenuCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
