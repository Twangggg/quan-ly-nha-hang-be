using FluentValidation;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public record UpdateSetMenuStockStatusCommand(Guid SetMenuId, bool IsOutOfStock) : IRequest<Result<UpdateSetMenuStockStatusResponse>>;

    public class UpdateSetMenuStockStatusValidator : AbstractValidator<UpdateSetMenuStockStatusCommand>
    {
        public UpdateSetMenuStockStatusValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("SetMenuId is required.");
        }
    }
}

