namespace FoodHub.Application.Features.Dashboard.Operational.Queries.GetOperationalStats
{
    public class GetOperationalStatsResponse
    {
        public int OccupiedTables { get; set; }
        public int TotalTables { get; set; }
        public double TableOccupancyRate { get; set; }
        public double TableTrend { get; set; }
        public int ActiveStaffCount { get; set; }
        public int TotalStaffOnShift { get; set; }
        public int StaffTrend { get; set; }
        public List<int> TableHistory { get; set; } = new();
        public List<int> StaffHistory { get; set; } = new();
    }
}
