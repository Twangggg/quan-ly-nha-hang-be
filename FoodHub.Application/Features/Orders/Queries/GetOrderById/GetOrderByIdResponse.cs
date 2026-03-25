using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Features.OrderItems.Common;
using FoodHub.Application.Features.Orders.Queries.GetOrders;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdResponse : IMapFrom<Order>
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string OrderType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid? TableId { get; set; }
        public Guid? ReservationId { get; set; }
        public string? Note { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid? PromotionId { get; set; }
        public string? PromotionCode { get; set; }
        public string? VoucherCode { get; set; }
        public Guid? GiftItemId { get; set; }
        public string? GiftItemName { get; set; }
        public int? GiftQuantity { get; set; }
        public bool IsPriority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();

        public void Mapping(Profile profile)
        {
            profile
                .CreateMap<Order, GetOrderByIdResponse>()
                .ForMember(d => d.OrderType, opt => opt.MapFrom(s => s.OrderType.ToString()))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PromotionCode, opt => opt.MapFrom(s => s.Promotion != null ? s.Promotion.Code : null))
                .ForMember(d => d.VoucherCode, opt => opt.MapFrom(s => s.Promotion != null ? s.Promotion.Code : null))
                .ForMember(d => d.GiftItemId, opt => opt.MapFrom(s => s.Promotion != null ? s.Promotion.ItemId : null))
                .ForMember(d => d.GiftItemName, opt => opt.MapFrom(s => s.Promotion != null && s.Promotion.Item != null ? s.Promotion.Item.Name : null))
                .ForMember(d => d.GiftQuantity, opt => opt.MapFrom(s => s.Promotion != null ? s.Promotion.FreeQuantity : null));
        }
    }
}
