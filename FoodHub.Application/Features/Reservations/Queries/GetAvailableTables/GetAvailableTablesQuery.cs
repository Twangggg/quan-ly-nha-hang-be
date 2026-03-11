using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Queries.GetAvailableTables
{
    public class GetAvailableTablesResponse
    {
        public Guid TableId { get; set; }
        public int TableNumber { get; set; }
        public int Capacity { get; set; }
        public Guid AreaId { get; set; }
    }

    public class GetAvailableTablesQuery : IRequest<Result<List<GetAvailableTablesResponse>>>
    {
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public Guid? AreaId { get; set; }
        public int GuestCount { get; set; }
    }
}
