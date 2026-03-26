namespace FoodHub.Application.Features.KDS.Queries.GetKdsBacklogSummary
{
    public class GetKdsBacklogSummaryResponse
    {
        public int TotalProcessingItems { get; set; }
        public int WaitingCount { get; set; }
        public int PreparingCount { get; set; }
        public int DelayedCount { get; set; }
        public double PreparingPercentage { get; set; }
    }
}
