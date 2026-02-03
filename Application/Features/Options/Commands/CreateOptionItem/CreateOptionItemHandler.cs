using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemHandler : IRequestHandler<CreateOptionItemCommand, Result<OptionItemDto>>
    {
        public CreateOptionItemHandler()
        {
        }

        public async Task<Result<OptionItemDto>> Handle(CreateOptionItemCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
