using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemHandler : IRequestHandler<CreateOptionItemCommand, Result<CreateOptionItemResponse>>
    {
        public CreateOptionItemHandler()
        {
        }

        public async Task<Result<CreateOptionItemResponse>> Handle(CreateOptionItemCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
