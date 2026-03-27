using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class KdsStationWipLimit
    {
        protected KdsStationWipLimit() { }

        public Guid KdsStationWipLimitId { get; private set; }
        public Guid KdsSettingsId { get; private set; }
        public Station Station { get; private set; }
        public int Limit { get; private set; }
        public bool Enabled { get; private set; }

        public static KdsStationWipLimit Create(Station station, int limit, bool enabled)
        {
            if (limit < 0)
            {
                throw new InvalidOperationException(
                    DomainErrors.KdsSettings.InvalidStationWipLimit
                );
            }

            return new KdsStationWipLimit
            {
                KdsStationWipLimitId = Guid.NewGuid(),
                Station = station,
                Limit = limit,
                Enabled = enabled,
            };
        }

        public DomainResult Update(int limit, bool enabled)
        {
            if (limit < 0)
            {
                return DomainResult.Failure(DomainErrors.KdsSettings.InvalidStationWipLimit);
            }

            Limit = limit;
            Enabled = enabled;
            return DomainResult.Success();
        }
    }
}
