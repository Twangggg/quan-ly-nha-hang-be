using System.ComponentModel.DataAnnotations;
using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommand : IRequest<Result<Guid>>, IMustBeActive
    {
        public OrderType OrderType { get; set; }
        // Required for DINE_IN (Walk-in)
        public Guid? TableId { get; set; }
        // Required for DINE_IN (Check-in from reservation)
        public Guid? ReservationId { get; set; }
        public string? Note { get; set; }
    }
}
