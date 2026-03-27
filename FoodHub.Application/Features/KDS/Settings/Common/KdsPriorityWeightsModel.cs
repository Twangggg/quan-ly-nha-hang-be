namespace FoodHub.Application.Features.KDS.Settings.Common
{
    public class KdsPriorityWeightsModel
    {
        public double WaitTimePerMinute { get; set; }
        public double OrderPriorityBonus { get; set; }
        public double ExpectedTimeWeight { get; set; }
        public double OverduePerMinute { get; set; }
        public double CompletionBoostWeight { get; set; }
        public double TakeawayBonus { get; set; }
        public double DeliveryBonus { get; set; }
    }
}
