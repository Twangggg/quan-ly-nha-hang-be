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
    }
}
