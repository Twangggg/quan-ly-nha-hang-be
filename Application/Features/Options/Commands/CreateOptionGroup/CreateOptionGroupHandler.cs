using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupHandler : IRequestHandler<CreateOptionGroupCommand, Result<CreateOptionGroupResponse>>
    {
        public CreateOptionGroupHandler()
        {
        }

        public async Task<Result<CreateOptionGroupResponse>> Handle(CreateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
