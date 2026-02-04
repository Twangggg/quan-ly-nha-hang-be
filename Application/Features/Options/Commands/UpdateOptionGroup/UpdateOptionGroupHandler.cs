using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupHandler : IRequestHandler<UpdateOptionGroupCommand, Result<UpdateOptionGroupResponse>>
    {
        public UpdateOptionGroupHandler()
        {
        }

        public async Task<Result<UpdateOptionGroupResponse>> Handle(UpdateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
