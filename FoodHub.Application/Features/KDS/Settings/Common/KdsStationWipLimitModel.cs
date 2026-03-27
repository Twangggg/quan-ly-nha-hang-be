using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Settings.Common
{
    public class KdsStationWipLimitModel
    {
        public Station Station { get; set; }
        public int Limit { get; set; }
        public bool Enabled { get; set; }
    }
}
