using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuHandler : IRequestHandler<UpdateSetMenuCommand, Result<SetMenuDto>>
    {
        public UpdateSetMenuHandler()
        {
        }

        public async Task<Result<SetMenuDto>> Handle(UpdateSetMenuCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
