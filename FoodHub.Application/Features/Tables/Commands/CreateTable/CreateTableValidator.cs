using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public class CreateTableValidator : AbstractValidator<CreateTableCommand>
    {
        public CreateTableValidator(IMessageService messageService)
        {
            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.Table.CapacityGreaterZero))
                .LessThanOrEqualTo(100).WithMessage(messageService.GetMessage(MessageKeys.Table.CapacityMaxLimit));

            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Table.AreaIdRequired));
        }
    }
}
