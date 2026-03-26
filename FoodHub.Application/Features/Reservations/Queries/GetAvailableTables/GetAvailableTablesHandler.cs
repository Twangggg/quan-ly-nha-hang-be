using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Reservations.Queries.GetAvailableTables
{
    public class GetAvailableTablesHandler
        : IRequestHandler<GetAvailableTablesQuery, Result<List<GetAvailableTablesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReservationSettingsProvider _reservationSettingsProvider;
        private readonly IReservationLifecyclePolicy _reservationLifecyclePolicy;

        public GetAvailableTablesHandler(
            IUnitOfWork unitOfWork,
            IReservationSettingsProvider reservationSettingsProvider,
            IReservationLifecyclePolicy reservationLifecyclePolicy
        )
        {
            _unitOfWork = unitOfWork;
            _reservationSettingsProvider = reservationSettingsProvider;
            _reservationLifecyclePolicy = reservationLifecyclePolicy;
        }

        public async Task<Result<List<GetAvailableTablesResponse>>> Handle(
            GetAvailableTablesQuery request,
            CancellationToken cancellationToken
        )
        {
            var query = _unitOfWork
                .Repository<Table>()
                .Query()
                .Where(t => t.Status != TableStatus.OutOfService);

            query = query.Where(t => t.Capacity >= request.GuestCount);

            if (request.AreaId.HasValue)
            {
                query = query.Where(t => t.AreaId == request.AreaId.Value);
            }

            var allTables = await query.ToListAsync(cancellationToken);

            var settings = await _reservationSettingsProvider.GetOrCreateAsync(cancellationToken);
            var now = _reservationLifecyclePolicy.GetBusinessNow();
            var buffer = TimeSpan.FromMinutes(settings.OverlapBufferMinutes);
            var minTime = request.ReservationTime.Subtract(buffer);
            var maxTime = request.ReservationTime.Add(buffer);

            var overlappingReservations = await _unitOfWork
                .Repository<Reservation>()
                .Query()
                .Where(r =>
                    r.ReservationDate == request.ReservationDate
                    && r.ReservationTime > minTime
                    && r.ReservationTime < maxTime
                )
                .ToListAsync(cancellationToken);
            var overlappingTableIds = overlappingReservations
                .Where(r => _reservationLifecyclePolicy.IsBlockingReservation(r, settings, now))
                .Select(r => r.TableId)
                .Distinct()
                .ToList();

            var availableTables = allTables
                .Where(t => !overlappingTableIds.Contains(t.TableId))
                .Select(t => new GetAvailableTablesResponse
                {
                    TableId = t.TableId,
                    TableNumber = t.TableNumber,
                    Capacity = t.Capacity,
                    AreaId = t.AreaId,
                })
                .ToList();

            return Result<List<GetAvailableTablesResponse>>.Success(availableTables);
        }
    }
}
