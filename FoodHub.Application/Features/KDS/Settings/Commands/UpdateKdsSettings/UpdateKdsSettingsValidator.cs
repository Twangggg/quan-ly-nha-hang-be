using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.KDS.Settings.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Settings.Commands.UpdateKdsSettings
{
    public class UpdateKdsSettingsValidator : AbstractValidator<UpdateKdsSettingsCommand>
    {
        public UpdateKdsSettingsValidator(IMessageService messageService)
        {
            RuleFor(x => x.SortMode)
                .IsInEnum()
                .WithMessage(messageService.GetMessage(MessageKeys.KdsSettings.InvalidSortMode));

            RuleFor(x => x.PriorityWeights).NotNull();

            When(x => x.PriorityWeights != null, () =>
            {
                RuleFor(x => x.PriorityWeights.WaitTimePerMinute)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );

                RuleFor(x => x.PriorityWeights.OrderPriorityBonus)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );

                RuleFor(x => x.PriorityWeights.ExpectedTimeWeight)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );

                RuleFor(x => x.PriorityWeights.OverduePerMinute)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );

                RuleFor(x => x.PriorityWeights.CompletionBoostWeight)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );

                RuleFor(x => x.PriorityWeights.TakeawayBonus)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );

                RuleFor(x => x.PriorityWeights.DeliveryBonus)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidPriorityWeight)
                    );
            });

            RuleFor(x => x.StationWipLimits)
                .NotEmpty()
                .WithMessage(
                    messageService.GetMessage(MessageKeys.KdsSettings.StationWipLimitsRequired)
                );

            RuleForEach(x => x.StationWipLimits).SetValidator(
                new KdsStationWipLimitModelValidator(messageService)
            );

            RuleFor(x => x.StationWipLimits)
                .Must(HaveUniqueStations)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.KdsSettings.DuplicateStationWipLimit)
                );

            RuleFor(x => x.StationWipLimits)
                .Must(ContainAllStations)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.KdsSettings.MissingStationWipLimit)
                );
        }

        private static bool HaveUniqueStations(List<KdsStationWipLimitModel> limits)
        {
            return limits.Select(x => x.Station).Distinct().Count() == limits.Count;
        }

        private static bool ContainAllStations(List<KdsStationWipLimitModel> limits)
        {
            var stations = limits.Select(x => x.Station).ToHashSet();
            return Enum.GetValues<Station>().All(stations.Contains);
        }

        private sealed class KdsStationWipLimitModelValidator
            : AbstractValidator<KdsStationWipLimitModel>
        {
            public KdsStationWipLimitModelValidator(IMessageService messageService)
            {
                RuleFor(x => x.Station)
                    .IsInEnum()
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidStationWipLimit)
                    );

                RuleFor(x => x.Limit)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.KdsSettings.InvalidStationWipLimit)
                    );
            }
        }
    }
}
