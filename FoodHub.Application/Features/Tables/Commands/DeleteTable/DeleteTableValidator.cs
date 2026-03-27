using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.DeleteTable
{
    public class DeleteTableValidator : AbstractValidator<DeleteTableCommand>
    {
        public DeleteTableValidator(IMessageService messageService)
        {
            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Table.IdRequired));
        }
    }
}
