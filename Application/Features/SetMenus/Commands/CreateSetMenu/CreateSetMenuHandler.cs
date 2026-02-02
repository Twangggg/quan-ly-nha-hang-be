using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuHandler : IRequestHandler<CreateSetMenuCommand, Result<SetMenuDto>>
    {
        public CreateSetMenuHandler()
        {
        }

        public async Task<Result<SetMenuDto>> Handle(CreateSetMenuCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
