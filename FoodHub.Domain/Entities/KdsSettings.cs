using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public sealed record KdsStationWipLimitConfig(Station Station, int Limit, bool Enabled);

    public class KdsSettings : BaseEntity
    {
        public const string DefaultSettingsKey = "kds";
        public const int DefaultWipLimit = 4;
        public const double DefaultWaitTimePerMinute = 2;
        public const double DefaultOrderPriorityBonus = 100;
        public const double DefaultExpectedTimeWeight = 1.5;
        public const double DefaultOverduePerMinute = 10;
        public const double DefaultCompletionBoostWeight = 50;
        public const double DefaultTakeawayBonus = 15;
        public const double DefaultDeliveryBonus = 25;
        public const KdsSortMode DefaultSortMode = KdsSortMode.Hybrid;

        protected KdsSettings() { }

        public Guid KdsSettingsId { get; private set; }
        public string SettingsKey { get; private set; } = DefaultSettingsKey;
        public KdsSortMode SortMode { get; private set; }
        public double WaitTimePerMinute { get; private set; }
        public double OrderPriorityBonus { get; private set; }
        public double ExpectedTimeWeight { get; private set; }
        public double OverduePerMinute { get; private set; }
        public double CompletionBoostWeight { get; private set; }
        public double TakeawayBonus { get; private set; }
        public double DeliveryBonus { get; private set; }

        public ICollection<KdsStationWipLimit> StationWipLimits { get; private set; } =
            new List<KdsStationWipLimit>();

        public static KdsSettings CreateDefault(Guid? createdBy = null)
        {
            var settings = new KdsSettings
            {
                KdsSettingsId = Guid.NewGuid(),
                SettingsKey = DefaultSettingsKey,
                SortMode = DefaultSortMode,
                WaitTimePerMinute = DefaultWaitTimePerMinute,
                OrderPriorityBonus = DefaultOrderPriorityBonus,
                ExpectedTimeWeight = DefaultExpectedTimeWeight,
                OverduePerMinute = DefaultOverduePerMinute,
                CompletionBoostWeight = DefaultCompletionBoostWeight,
                TakeawayBonus = DefaultTakeawayBonus,
                DeliveryBonus = DefaultDeliveryBonus,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };

            foreach (var station in Enum.GetValues<Station>())
            {
                settings.StationWipLimits.Add(
                    KdsStationWipLimit.Create(station, DefaultWipLimit, enabled: true)
                );
            }

            return settings;
        }

        public DomainResult Update(
            KdsSortMode sortMode,
            double waitTimePerMinute,
            double orderPriorityBonus,
            double expectedTimeWeight,
            double overduePerMinute,
            double completionBoostWeight,
            double takeawayBonus,
            double deliveryBonus,
            IEnumerable<KdsStationWipLimitConfig> stationWipLimits,
            Guid? updatedBy = null
        )
        {
            var configs = stationWipLimits.ToList();

            if (configs.Count == 0)
            {
                return DomainResult.Failure(DomainErrors.KdsSettings.StationWipLimitsRequired);
            }

            if (configs.GroupBy(x => x.Station).Any(x => x.Count() > 1))
            {
                return DomainResult.Failure(DomainErrors.KdsSettings.DuplicateStationWipLimit);
            }

            if (
                waitTimePerMinute < 0
                || orderPriorityBonus < 0
                || expectedTimeWeight < 0
                || overduePerMinute < 0
                || completionBoostWeight < 0
                || takeawayBonus < 0
                || deliveryBonus < 0
            )
            {
                return DomainResult.Failure(DomainErrors.KdsSettings.InvalidPriorityWeight);
            }

            if (configs.Any(x => x.Limit < 0))
            {
                return DomainResult.Failure(DomainErrors.KdsSettings.InvalidStationWipLimit);
            }

            SortMode = sortMode;
            WaitTimePerMinute = waitTimePerMinute;
            OrderPriorityBonus = orderPriorityBonus;
            ExpectedTimeWeight = expectedTimeWeight;
            OverduePerMinute = overduePerMinute;
            CompletionBoostWeight = completionBoostWeight;
            TakeawayBonus = takeawayBonus;
            DeliveryBonus = deliveryBonus;

            var existingLimits = StationWipLimits.ToDictionary(x => x.Station);
            var configuredStations = new HashSet<Station>();

            foreach (var config in configs)
            {
                configuredStations.Add(config.Station);

                if (existingLimits.TryGetValue(config.Station, out var existingLimit))
                {
                    var updateResult = existingLimit.Update(config.Limit, config.Enabled);
                    if (!updateResult.IsSuccess)
                    {
                        return updateResult;
                    }
                }
                else
                {
                    StationWipLimits.Add(
                        KdsStationWipLimit.Create(config.Station, config.Limit, config.Enabled)
                    );
                }
            }

            var staleLimits = StationWipLimits
                .Where(x => !configuredStations.Contains(x.Station))
                .ToList();

            foreach (var staleLimit in staleLimits)
            {
                StationWipLimits.Remove(staleLimit);
            }

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public bool EnsureDefaultStationWipLimits()
        {
            var hasChanges = false;
            var existingStations = StationWipLimits.Select(x => x.Station).ToHashSet();

            foreach (var station in Enum.GetValues<Station>())
            {
                if (existingStations.Contains(station))
                {
                    continue;
                }

                StationWipLimits.Add(
                    KdsStationWipLimit.Create(station, DefaultWipLimit, enabled: true)
                );
                hasChanges = true;
            }

            return hasChanges;
        }

        public int? ResolveWipLimit(string stationSnapshot)
        {
            if (!Enum.TryParse<Station>(stationSnapshot, ignoreCase: true, out var station))
            {
                return DefaultWipLimit;
            }

            var stationLimit = StationWipLimits.FirstOrDefault(x => x.Station == station);
            if (stationLimit == null)
            {
                return DefaultWipLimit;
            }

            return stationLimit.Enabled ? stationLimit.Limit : null;
        }
    }
}
