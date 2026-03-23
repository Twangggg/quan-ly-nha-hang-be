using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucher
{
    public class UpdateVoucherResponse : IMapFrom<Voucher>
    {
        public Guid VoucherId { get; set; } // Dùng để tham chiếu, không phải là khóa chính
        public string VoucherCode { get; set; } // Nhập vào lúc tạo voucher, có thể dùng làm mã giảm giá và phải là duy nhất, có thể được hiển thị cho khách hàng để áp dụng khi đặt hàng
        public VoucherType VoucherType { get; set; }
        public string VoucherTypeName { get; set; } // Tên hiển thị của loại voucher, ví dụ: "Giảm theo phần trăm", "Giảm theo số tiền", "Tặng món ăn miễn phí"
        public decimal? DiscountValue { get; set; } // Giá trị giảm giá, có thể là phần trăm hoặc số tiền tùy thuộc vào VoucherType, null khi là FreeItem
        public decimal? MaxDiscount { get; set; } // Áp dụng cho các voucher giảm theo phần trăm để giới hạn số tiền giảm tối đa
        public decimal? MinOrderValue { get; set; } // Giá trị đơn hàng tối thiểu để áp dụng voucher
        public Guid? ItemtId { get; set; } // Áp dụng cho voucher loại FreeItem, tham chiếu đến món ăn được tặng miễn phí, nếu sản phẩm có sẵn trong order thì sẽ miễn phí theo số lượng FreeQuantity, nếu không có sẵn thì sẽ thêm vào order với số lượng FreeQuantity và giá bằng 0
        public string? ItemName { get; set; }
        public int? FreeQuantity { get; set; } // Số lượng món ăn được tặng miễn phí, chỉ áp dụng cho voucher loại FreeItem

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public TimeSpan? StartTime { get; set; } // Thời gian bắt đầu trong ngày để áp dụng voucher, null có nghĩa là không giới hạn thời gian trong ngày
        public TimeSpan? EndTime { get; set; } // Thời gian kết thúc trong ngày để áp dụng voucher, null có nghĩa là không giới hạn thời gian trong ngày

        public bool IsActive { get; set; } // Trạng thái kích hoạt của voucher, chỉ những voucher có IsActive = true mới có thể được áp dụng

        public int? UsageLimit { get; set; } // Số lần voucher có thể được sử dụng, 0 hoặc null có nghĩa là không giới hạn

        public int UsedCount { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Voucher, UpdateVoucherResponse>()
                .ForMember(dest => dest.VoucherTypeName, opt => opt.MapFrom(src => src.VoucherType.ToString()))
                .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.Item != null ? src.Item.Name : null));
        }
    }
}
