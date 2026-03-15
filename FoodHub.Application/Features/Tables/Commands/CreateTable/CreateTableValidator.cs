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
            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("Capacity must be less than or equal to 100.");

            RuleFor(x => x.AreaId)
                .NotEmpty().WithMessage("AreaId is required.");
        }
    }
}
