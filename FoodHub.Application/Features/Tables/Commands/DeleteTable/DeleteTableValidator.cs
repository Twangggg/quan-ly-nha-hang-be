using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.DeleteTable
{
    public class DeleteTableValidator : AbstractValidator<DeleteTableCommand>
    {
        public DeleteTableValidator()
        {
            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage("Table id is required.");
        }
    }
}
