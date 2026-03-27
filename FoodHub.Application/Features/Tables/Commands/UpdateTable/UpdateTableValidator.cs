using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTable
{
    public class UpdateTableValidator : AbstractValidator<UpdateTableCommand>
    {
        public UpdateTableValidator(IMessageService messageService)
        {
            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Table.IdRequired));
            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Table.CapacityGreaterZero))
                .LessThanOrEqualTo(100).WithMessage(messageService.GetMessage(MessageKeys.Table.CapacityMaxLimit));
            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Table.AreaIdRequired));
        }
    }
}
