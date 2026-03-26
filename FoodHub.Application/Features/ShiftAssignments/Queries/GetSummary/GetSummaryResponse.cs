namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetSummary
{
    public class GetSummaryResponse
    {
        public int TotalEmployees { get; set; }
        public double EstimatedHours { get; set; }
        public decimal EstimatedCost { get; set; }
        public double CoveragePercentage { get; set; }
    }
}
