using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Promotions.Commands.UpdatePromotion
{
    public class UpdatePromotionValidator : AbstractValidator<UpdatePromotionCommand>
    {
        public UpdatePromotionValidator(IMessageService messageService)
        {
            RuleFor(x => x.PromotionId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "PromotionId" }));

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidFormat, new { Field = "Code" }))
                .MaximumLength(50)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidFormat, new { Field = "Type" }));

            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.Value)
                .GreaterThan(0)
                .When(x => x.Type != PromotionType.FreeItem)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.MaxDiscount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MaxDiscount.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.MinOrderValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MinOrderValue.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidDate));

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidDate));

            RuleFor(x => x)
                .Must(x => x.EndDate >= x.StartDate)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidDate));

            RuleFor(x => x)
                .Must(x => x.StartTime.HasValue == x.EndTime.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x)
                .Must(x => !x.StartTime.HasValue || !x.EndTime.HasValue || x.EndTime >= x.StartTime)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.ItemId)
                .NotEmpty()
                .When(x => x.Type == PromotionType.FreeItem)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.FreeQuantity)
                .GreaterThan(0)
                .When(x => x.Type == PromotionType.FreeItem)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));

            RuleFor(x => x.UsageLimit)
                .GreaterThan(0)
                .When(x => x.UsageLimit.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Voucher.Invalid));
        }
    }
}
