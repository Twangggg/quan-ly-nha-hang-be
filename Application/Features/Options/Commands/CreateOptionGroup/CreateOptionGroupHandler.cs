using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupHandler : IRequestHandler<CreateOptionGroupCommand, Result<OptionGroupDto>>
    {
        public CreateOptionGroupHandler()
        {
        }

        public async Task<Result<OptionGroupDto>> Handle(CreateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
