using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Reservations.Queries.GetAvailableTables
{
    public class GetAvailableTablesHandler : IRequestHandler<GetAvailableTablesQuery, Result<List<GetAvailableTablesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAvailableTablesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<GetAvailableTablesResponse>>> Handle(GetAvailableTablesQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<Table>().Query();

            // Lọc theo sức chứa
            query = query.Where(t => t.Capacity >= request.GuestCount);

            // Lọc theo khu vực nếu có
            if (request.Area.HasValue)
            {
                query = query.Where(t => t.Area == request.Area.Value);
            }

            var allTables = await query.ToListAsync(cancellationToken);

            // Check overlapping reservations (chênh lệch dưới 2 tiếng)
            var bufferHours = 2;
            var minTime = request.ReservationTime.Subtract(TimeSpan.FromHours(bufferHours));
            var maxTime = request.ReservationTime.Add(TimeSpan.FromHours(bufferHours));

            var overlappingReservations = await _unitOfWork.Repository<Reservation>().Query()
                .Where(r => r.ReservationDate == request.ReservationDate 
                            && r.Status == ReservationStatus.Booked
                            && r.ReservationTime > minTime 
                            && r.ReservationTime < maxTime)
                .Select(r => r.TableId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var availableTables = allTables
                .Where(t => !overlappingReservations.Contains(t.TableId))
                .Select(t => new GetAvailableTablesResponse
                {
                    TableId = t.TableId,
                    TableNumber = t.TableNumber,
                    Capacity = t.Capacity,
                    Area = t.Area
                })
                .ToList();

            return Result<List<GetAvailableTablesResponse>>.Success(availableTables);
        }
    }
}
