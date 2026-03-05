using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTableStatus
{
    public class UpdateTableStatusValidator : AbstractValidator<UpdateTableStatusCommand>
    {
        public UpdateTableStatusValidator()
        {
            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage("TableId is required.");
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid status value.");
        }
    }
}
