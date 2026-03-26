using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Billing.Queries.GetBillingHistory
{
    public class GetBillingHistoryResponse : IMapFrom<Order>
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string OrderType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid? TableId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Order, GetBillingHistoryResponse>()
                .ForMember(d => d.OrderType,
                    opt => opt.MapFrom(s => s.OrderType.ToString()))
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentMethod,
                    opt => opt.MapFrom(s => s.OrderPayments.Any() 
                        ? string.Join(", ", s.OrderPayments.Select(p => p.PaymentMethodConfig.Name)) 
                        : (s.PaymentMethod != null ? s.PaymentMethod.ToString() : null)));
        }
    }
}
