using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport
{
    public class ExportAttendanceReportHandler : IRequestHandler<ExportAttendanceReportQuery, Result<byte[]>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAttendanceExcelService _excelService;
        private readonly ILogger<ExportAttendanceReportHandler> _logger;

        public ExportAttendanceReportHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IAttendanceExcelService excelService,
            ILogger<ExportAttendanceReportHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _excelService = excelService;
            _logger = logger;
        }

        public async Task<Result<byte[]>> Handle(ExportAttendanceReportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start exporting Attendance Report to Excel with Filters: {Filters}", string.Join(", ", request.Pagination.Filters ?? new List<string>()));

            var query = _unitOfWork.Repository<Attendance>().Query()
                .Include(a => a.Employee)
                .Include(a => a.ShiftAssignment)
                    .ThenInclude(sa => sa.Shift)
                .AsNoTracking();

            var searchableFields = new List<Expression<Func<Attendance, string?>>>
            {
                a => a.Employee.FullName,
                a => a.Employee.EmployeeCode
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Attendance, object?>>>
            {
                { "employeeid", a => a.EmployeeId },
                { "islate", a => a.isLate },
                { "isearlyleave", a => a.isEarlyLeave },
                { "date", a => a.CheckInTime.Date }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Attendance, object?>>>
            {
                { "date", a => a.CheckInTime.Date },
                { "employeename", a => a.Employee.FullName },
                { "checkintime", a => a.CheckInTime },
                { "checkouttime", a => a.CheckOutTime }
            };

            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, a => a.CheckInTime);

            var items = await query
                .ProjectTo<GetAttendanceReportResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.Date = item.AssignedDate ?? DateOnly.FromDateTime(item.CheckInTime.AddHours(7));
                item.Status = item.CheckOutTime == null ? "Thiếu giờ ra" 
                            : (item.isLate == true && item.isEarlyLeave == true) ? "Đi trễ & Về sớm"
                            : item.isLate == true ? "Đi trễ"
                            : item.isEarlyLeave == true ? "Về sớm" : "Đúng giờ";
            }

            var fileContent = _excelService.ExportAttendanceReportToExcel(items);

            _logger.LogInformation("Successfully exported Attendance Report to Excel with {Count} items", items.Count);

            return Result<byte[]>.Success(fileContent);
        }
    }
}
