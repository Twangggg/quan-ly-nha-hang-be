using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationCommand : IRequest<Result<CreateReservationResponse>>
    {
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public PartyType PartyType { get; set; }
        public int GuestCount { get; set; }
        public string? Note { get; set; }
        public Guid AreaId { get; set; }
        
        // Pre-order
        public List<PreOrderItemRequest>? PreOrderItems { get; set; }
        public List<PreOrderSetMenuRequest>? PreOrderSetMenus { get; set; }
    }

    public class PreOrderItemRequest
    {
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public List<PreOrderItemOptionGroupDto>? SelectedOptions { get; set; }
    }

    public class PreOrderItemOptionGroupDto
    {
        public Guid OptionGroupId { get; set; }
        public List<PreOrderItemOptionValueDto> SelectedValues { get; set; } = new();
    }

    public class PreOrderItemOptionValueDto
    {
        public Guid OptionItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }

    public class PreOrderSetMenuRequest
    {
        public Guid SetMenuId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }

    public class CreateReservationResponse
    {
        public Guid ReservationId { get; set; }
        public Guid TableId { get; set; }
    }
}
