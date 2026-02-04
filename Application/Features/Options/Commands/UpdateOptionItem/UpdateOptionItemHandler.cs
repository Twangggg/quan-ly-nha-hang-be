using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemHandler : IRequestHandler<UpdateOptionItemCommand, Result<UpdateOptionItemResponse>>
    {
        public UpdateOptionItemHandler()
        {
        }

        public async Task<Result<UpdateOptionItemResponse>> Handle(UpdateOptionItemCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
