using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Vouchers.Queries.GetVouchers
{
    public class GetVouchersResponse : IMapFrom<Voucher>
    {
        public Guid VoucherId { get; set; }
        public string VoucherCode { get; set; }
        public VoucherType VoucherType { get; set; }
        public string VoucherTypeName { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public Guid? ItemtId { get; set; }
        public string? ItemName { get; set; }
        public int? FreeQuantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Voucher, GetVouchersResponse>()
                .ForMember(dest => dest.VoucherTypeName, opt => opt.MapFrom(src => src.VoucherType.ToString()))
                .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.Item != null ? src.Item.Name : null));
        }
    }
}
