using FoodHub.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace FoodHub.Application.Features.Reservations.Queries.GetReservations
{
    public class GetReservationsQuery : PaginationParams, IRequest<Result<PagedResult<ReservationDto>>>
    {
        public DateOnly? Date { get; set; }
        public Guid? AreaId { get; set; }
        public string? Status { get; set; }
    }

    public class ReservationDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public Guid? AreaId { get; set; }
        public string Area { get; set; } = string.Empty;
        public int People { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
