using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetRevenueChart
{
    public class GetRevenueChartHandler
        : IRequestHandler<GetRevenueChartQuery, Result<GetRevenueChartResponse>>
    {
        private static readonly TimeZoneInfo _vietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetRevenueChartHandler> _logger;

        public GetRevenueChartHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetRevenueChartHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetRevenueChartResponse>> Handle(
            GetRevenueChartQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Getting revenue chart for Date={Date}, Year={Year}, Month={Month}",
                request.Date,
                request.Year,
                request.Month
            );

            var response = new GetRevenueChartResponse();

            if (request.Date.HasValue)
            {
                // Hourly breakdown for a specific day
                var date = request.Date.Value;
                var startLocal = date.ToDateTime(TimeOnly.MinValue);
                var endLocal = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _vietnamTz);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _vietnamTz);

                var orders = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .AsNoTracking()
                    .Where(o =>
                        o.PaidAt >= startUtc
                        && o.PaidAt < endUtc
                        && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                    )
                    .Select(o => new { o.PaidAt, o.TotalAmount })
                    .ToListAsync(cancellationToken);

                // Initialize 24 hours
                for (int i = 0; i < 24; i++)
                {
                    var hourLabel = $"{i:D2}:00";
                    var hourlyRevenue = orders
                        .Where(o =>
                            TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt!.Value, _vietnamTz).Hour == i
                        )
                        .Sum(o => o.TotalAmount);

                    response.Points.Add(
                        new RevenuePointDto { Label = hourLabel, Revenue = hourlyRevenue }
                    );
                }
            }
            else if (request.Year.HasValue && request.Month.HasValue)
            {
                // Daily breakdown for a month
                int daysInMonth = DateTime.DaysInMonth(request.Year.Value, request.Month.Value);
                var startLocal = new DateTime(request.Year.Value, request.Month.Value, 1);
                var endLocal = startLocal.AddMonths(1);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _vietnamTz);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _vietnamTz);

                var orders = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .AsNoTracking()
                    .Where(o =>
                        o.PaidAt >= startUtc
                        && o.PaidAt < endUtc
                        && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                    )
                    .Select(o => new { o.PaidAt, o.TotalAmount })
                    .ToListAsync(cancellationToken);

                for (int i = 1; i <= daysInMonth; i++)
                {
                    var dayLabel = $"{i:D2}/{request.Month.Value:D2}";
                    var dailyRevenue = orders
                        .Where(o =>
                            TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt!.Value, _vietnamTz).Day == i
                        )
                        .Sum(o => o.TotalAmount);

                    response.Points.Add(
                        new RevenuePointDto { Label = dayLabel, Revenue = dailyRevenue }
                    );
                }
            }

            _logger.LogInformation(
                "Successfully generated revenue chart with {Count} points",
                response.Points.Count
            );

            return Result<GetRevenueChartResponse>.Success(response);
        }
    }
}
