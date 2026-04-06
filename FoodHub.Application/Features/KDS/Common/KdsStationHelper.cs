using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Common
{
    public static class KdsStationHelper
    {
        public static List<string> ExpandRequestedStations(string station)
        {
            var targetStations = new List<string> { station };
            if (station.Equals("Kitchen", StringComparison.OrdinalIgnoreCase))
            {
                targetStations.Add(Station.HotKitchen.ToString());
                targetStations.Add(Station.ColdKitchen.ToString());
            }

            return targetStations;
        }

        public static int GetWipLimitForStation(KdsSettings settings, string stationKey)
        {
            if (settings == null)
            {
                return KdsSettings.DefaultWipLimit;
            }

            var resolvedLimit = settings.ResolveWipLimit(stationKey);
            return resolvedLimit ?? KdsSettings.DefaultWipLimit;
        }
    }
}
