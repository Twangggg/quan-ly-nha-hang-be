using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Dashboard.Operational.Queries.GetOperationalStats
{
    public class GetOperationalStatsHandler
        : IRequestHandler<GetOperationalStatsQuery, Result<GetOperationalStatsResponse>>
    {
        private static readonly TimeZoneInfo _vietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;

        public GetOperationalStatsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<GetOperationalStatsResponse>> Handle(
            GetOperationalStatsQuery request,
            CancellationToken cancellationToken
        )
        {
            var today = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTz)
            );

            var totalTables = await _unitOfWork
                .Repository<Table>()
                .Query()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var occupiedTables = await _unitOfWork
                .Repository<Table>()
                .Query()
                .AsNoTracking()
                .CountAsync(t => t.Status == TableStatus.Occupied, cancellationToken);

            var tableOccupancyRate =
                totalTables > 0 ? Math.Round((double)occupiedTables / totalTables * 100, 2) : 0;

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var occupiedTablesLastHour = await _unitOfWork
                .Repository<Table>()
                .Query()
                .AsNoTracking()
                .CountAsync(
                    t => t.UpdatedAt >= oneHourAgo && t.Status == TableStatus.Occupied,
                    cancellationToken
                );

            var tableTrend =
                occupiedTablesLastHour > 0 && occupiedTables > 0
                    ? Math.Round(
                        (double)(occupiedTables - occupiedTablesLastHour)
                            / occupiedTablesLastHour
                            * 100,
                        2
                    )
                    : 0;

            var totalStaffOnShift = await _unitOfWork
                .Repository<ShiftAssignment>()
                .Query()
                .AsNoTracking()
                .CountAsync(
                    sa => sa.AssignedDate == today && sa.DeletedAt == null,
                    cancellationToken
                );

            var activeStaffCount = await _unitOfWork
                .Repository<ShiftAssignment>()
                .Query()
                .AsNoTracking()
                .Include(sa => sa.Employee)
                .CountAsync(
                    sa =>
                        sa.AssignedDate == today
                        && sa.DeletedAt == null
                        && sa.Employee.Status == EmployeeStatus.Active,
                    cancellationToken
                );

            var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
            var staffChangeLast30Min = await _unitOfWork
                .Repository<ShiftAssignment>()
                .Query()
                .AsNoTracking()
                .CountAsync(
                    sa => sa.UpdatedAt >= thirtyMinutesAgo && sa.AssignedDate == today,
                    cancellationToken
                );

            var staffTrend = staffChangeLast30Min;

            // Generate history for the last 10 periods (e.g. 10 hours)
            var tableHistory = new List<int>();
            var staffHistory = new List<int>();

            for (int i = 9; i >= 0; i--)
            {
                var pointInTime = DateTime.UtcNow.AddHours(-i);
                var startOfHour = pointInTime.AddMinutes(-30);
                var endOfHour = pointInTime.AddMinutes(30);

                // Approximate tables: Orders created around that time and not yet finished
                var tablesAtTime = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .AsNoTracking()
                    .CountAsync(
                        o =>
                            o.OrderType == OrderType.DineIn
                            && o.CreatedAt <= pointInTime
                            && (o.PaidAt == null || o.PaidAt > pointInTime),
                        cancellationToken
                    );

                tableHistory.Add(tablesAtTime);

                // Staff: Assignments active at that time
                var staffAtTime = await _unitOfWork
                    .Repository<ShiftAssignment>()
                    .Query()
                    .AsNoTracking()
                    .CountAsync(
                        sa => sa.CreatedAt <= pointInTime && sa.DeletedAt == null,
                        cancellationToken
                    );

                staffHistory.Add(staffAtTime);
            }

            return Result<GetOperationalStatsResponse>.Success(
                new GetOperationalStatsResponse
                {
                    OccupiedTables = occupiedTables,
                    TotalTables = totalTables,
                    TableOccupancyRate = tableOccupancyRate,
                    TableTrend = tableTrend,
                    ActiveStaffCount = activeStaffCount,
                    TotalStaffOnShift =
                        totalStaffOnShift > 0 ? totalStaffOnShift : activeStaffCount,
                    StaffTrend = staffTrend,
                    TableHistory = tableHistory,
                    StaffHistory = staffHistory,
                }
            );
        }
    }
}
