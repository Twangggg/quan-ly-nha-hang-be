using System.ComponentModel.DataAnnotations;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommand : IRequest<Result<Guid>>
    {
        public OrderType OrderType { get; set; }
        // Required for DINE_IN
        public Guid? TableId { get; set; }
        public string? Note { get; set; }
    }
}
