using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetSummary
{
    public class GetSummaryHandler : IRequestHandler<GetSummaryQuery, Result<GetSummaryResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public GetSummaryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<GetSummaryResponse>> Handle(GetSummaryQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetRequiredUserIdAsGuid();

            var shiftAssignmentRepo = _unitOfWork.Repository<ShiftAssignment>();

            // Query basic shift assignments within the date range
            var query = shiftAssignmentRepo.Query()
                .Include(sa => sa.Shift)
                .Where(sa => sa.AssignedDate >= request.StartDate && sa.AssignedDate <= request.EndDate);

            var assignments = await query.ToListAsync(cancellationToken);

            var response = new GetSummaryResponse
            {
                TotalEmployees = assignments.Select(sa => sa.EmployeeId).Distinct().Count(),
                EstimatedHours = 0,
                EstimatedCost = 0,
                CoveragePercentage = 0
            };

            // Tính tổng số giờ làm việc dự kiến dựa trên ca đã được phân công
            double totalHours = 0;
            const decimal BASE_HOURLY_WAGE = 50000m; // Giả sử mức lương cơ bản là 50,000 VND/giờ, có thể điều chỉnh theo thực tế

            foreach (var assignment in assignments)
            {
                if (assignment.Shift != null)
                {
                    var start = assignment.Shift.StartTime;
                    var end = assignment.Shift.EndTime;

                    double hours = (end - start).TotalHours;

                    totalHours += hours;
                }
            }

            response.EstimatedHours = Math.Round(totalHours, 2);
            response.EstimatedCost = (decimal)totalHours * BASE_HOURLY_WAGE;

            // Tính tỷ lệ lấp đầy ca làm việc (coverage percentage)
            // Tính tổng số ngày trong khoảng thời gian đã chọn
            int totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;

            // Tính số ca cần lấp đầy trong khoảng thời gian đã chọn (giả sử mỗi ngày có một số ca nhất định)
            var shiftsCount = await _unitOfWork.Repository<Shift>()
                .Query()
                .Where(s => s.Status == ShiftStatus.Active)
                .CountAsync(cancellationToken);

            // Tổng số ca cần được lấp đầy trong khoảng thời gian đã chọn
            int totalRequiredCapacity = totalDays * shiftsCount;

            if (totalRequiredCapacity > 0)
            {
                double coverage = (double)assignments.Count / totalRequiredCapacity * 100;
                response.CoveragePercentage = Math.Min(100, Math.Round(coverage, 2));
            }

            return Result<GetSummaryResponse>.Success(response);
        }
    }
}
