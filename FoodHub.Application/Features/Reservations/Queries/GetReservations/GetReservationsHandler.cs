using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Reservations.Queries.GetReservations
{
    public class GetReservationsHandler
        : IRequestHandler<GetReservationsQuery, Result<PagedResult<ReservationDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReservationsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<ReservationDto>>> Handle(
            GetReservationsQuery request,
            CancellationToken cancellationToken
        )
        {
            var query = _unitOfWork
                .Repository<Reservation>()
                .Query()
                .Include(r => r.Area)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(r =>
                    r.CustomerPhone.Contains(search) || r.CustomerName.ToLower().Contains(search)
                );
            }

            if (request.Date.HasValue)
            {
                query = query.Where(r => r.ReservationDate == request.Date.Value);
            }

            if (request.AreaId.HasValue)
            {
                query = query.Where(r => r.AreaId == request.AreaId.Value);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var inputStatus = request.Status.ToUpper();
                if (inputStatus != "ALL")
                {
                    ReservationStatus parsedStatus = ReservationStatus.Booked;
                    switch (inputStatus)
                    {
                        case "BOOKED":
                            parsedStatus = ReservationStatus.Booked;
                            break;
                        case "CHECKED_IN":
                            parsedStatus = ReservationStatus.CheckIn;
                            break;
                        case "CANCELLED":
                            parsedStatus = ReservationStatus.Cancelled;
                            break;
                        case "NO_SHOW":
                            parsedStatus = ReservationStatus.Cancelled;
                            break;
                    }
                    query = query.Where(r => r.Status == parsedStatus);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var reservations = await query
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.ReservationTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = reservations.Select(MapToDto).ToList();

            var pagedResult = new PagedResult<ReservationDto>(items, request, totalCount);

            return Result<PagedResult<ReservationDto>>.Success(pagedResult);
        }

        private ReservationDto MapToDto(Reservation r)
        {
            return new ReservationDto
            {
                Id = r.ReservationId,
                Code = "RES-" + r.ReservationId.ToString().Substring(0, 6).ToUpper(),
                CustomerName = r.CustomerName,
                Phone = r.CustomerPhone,
                Date = r.ReservationDate.ToString("yyyy-MM-dd"),
                Time = r.ReservationTime.ToString(@"hh\:mm"),
                AreaId = r.AreaId,
                Area = r.Area?.Name ?? "N/A",
                People = r.GuestCount,
                Status = MapStatus(r.Status),
            };
        }

        private string MapStatus(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Booked => "BOOKED",
                ReservationStatus.CheckIn => "CHECKED_IN",
                ReservationStatus.Cancelled => "CANCELLED",
                ReservationStatus.NoShow => "NO_SHOW",
                _ => "UNKNOWN",
            };
        }
    }
}
