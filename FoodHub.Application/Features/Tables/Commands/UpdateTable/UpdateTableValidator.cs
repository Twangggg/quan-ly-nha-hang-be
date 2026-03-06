using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTable
{
    public class UpdateTableValidator : AbstractValidator<UpdateTableCommand>
    {
        public UpdateTableValidator() {
            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage("Table id is required.");
            RuleFor(x => x.TableNumber)
                .GreaterThan(0).WithMessage("Table number must be greater than 0.");
            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.")
                .LessThanOrEqualTo(6).WithMessage("Capacity must be less than or equal to 6.");
            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage("Area id is required.");
        }
    }
}
