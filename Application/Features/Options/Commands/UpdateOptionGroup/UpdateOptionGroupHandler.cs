using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupHandler : IRequestHandler<UpdateOptionGroupCommand, Result<OptionGroupDto>>
    {
        public UpdateOptionGroupHandler()
        {
        }

        public async Task<Result<OptionGroupDto>> Handle(UpdateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
