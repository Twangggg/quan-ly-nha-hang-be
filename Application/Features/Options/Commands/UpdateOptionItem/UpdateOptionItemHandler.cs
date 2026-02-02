using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemHandler : IRequestHandler<UpdateOptionItemCommand, Result<OptionItemDto>>
    {
        public UpdateOptionItemHandler()
        {
        }

        public async Task<Result<OptionItemDto>> Handle(UpdateOptionItemCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
