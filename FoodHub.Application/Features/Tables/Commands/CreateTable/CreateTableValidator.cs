using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public class CreateTableValidator : AbstractValidator<CreateTableCommand>
    {
        public CreateTableValidator()
        {
            RuleFor(x => x.TableCode)
                .NotEmpty().WithMessage("Table code is required.")
                .MaximumLength(50).WithMessage("Table code must not exceed 50 characters.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage("AreaId is required.");
        }
    }
}
