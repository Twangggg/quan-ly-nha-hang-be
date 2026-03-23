using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Voucher : BaseEntity
    {
        public Guid VoucherId { get; set; } // Dùng để tham chiếu, không phải là khóa chính
        public string VoucherCode { get; set; } // Nhập vào lúc tạo voucher, có thể dùng làm mã giảm giá và phải là duy nhất, có thể được hiển thị cho khách hàng để áp dụng khi đặt hàng
        public VoucherType VoucherType { get; set; }
        public decimal? DiscountValue { get; set; } // Giá trị giảm giá, có thể là phần trăm hoặc số tiền tùy thuộc vào VoucherType, null khi là FreeItem
        public decimal? MaxDiscount { get; set; } // Áp dụng cho các voucher giảm theo phần trăm để giới hạn số tiền giảm tối đa
        public decimal? MinOrderValue { get; set; } // Giá trị đơn hàng tối thiểu để áp dụng voucher
        public Guid? ItemId { get; set; } // Áp dụng cho voucher loại FreeItem, tham chiếu đến món ăn được tặng miễn phí, nếu sản phẩm có sẵn trong order thì sẽ miễn phí theo số lượng FreeQuantity, nếu không có sẵn thì sẽ thêm vào order với số lượng FreeQuantity và giá bằng 0
        public virtual MenuItem? Item { get; set; } // Quan hệ nhiều-một với Item, mỗi voucher có thể áp dụng cho một món ăn cụ thể (chỉ áp dụng cho voucher loại FreeItem)
        public int? FreeQuantity { get; set; } // Số lượng món ăn được tặng miễn phí, chỉ áp dụng cho voucher loại FreeItem
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; } // Thời gian bắt đầu trong ngày để áp dụng voucher, null có nghĩa là không giới hạn thời gian trong ngày
        public TimeSpan? EndTime { get; set; } // Thời gian kết thúc trong ngày để áp dụng voucher, null có nghĩa là không giới hạn thời gian trong ngày
        public bool IsActive { get; set; } // Trạng thái kích hoạt của voucher, chỉ những voucher có IsActive = true mới có thể được áp dụng
        public int? UsageLimit { get; set; } // Số lần voucher có thể được sử dụng, 0 hoặc null có nghĩa là không giới hạn
        public int UsedCount { get; set; } // Số lần voucher đã được sử dụng, cần được cập nhật mỗi khi voucher được áp dụng thành công
        public virtual ICollection<Order> Orders { get; set; } // Quan hệ một-nhiều với Order, mỗi voucher có thể áp dụng cho nhiều đơn hàng

        // Các phương thức liên quan đến logic của voucher
        public bool IsValid()
        {
            var now = DateTime.UtcNow;
            return IsActive
                && now >= StartDate && now <= EndDate
                && (UsageLimit == null || UsedCount < UsageLimit)
                && (StartTime == null || now.TimeOfDay >= StartTime)
                && (EndTime == null || now.TimeOfDay <= EndTime);
        }

        public void Used(Guid auditorId)
        {
            UsedCount++;

            UpdatedBy = auditorId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnUsed(Guid auditorId)
        {
            if (UsedCount > 0)
            {
                UsedCount--;
            }
            UpdatedBy = auditorId;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsBelowMinAmount(decimal totalAmount)
        {

            if (MinOrderValue != null && totalAmount < MinOrderValue)
            {
                return true;
            }
            return false;
        }

        public bool IsFreeItemInOrder(Order order)
        {
            if (VoucherType != VoucherType.FreeItem || ItemId == null)
            {
                return false; // Không phải voucher tặng món hoặc không có ItemId
            }
            return order.OrderItems.Any(oi => oi.MenuItemId == ItemId);
        }
    }
}
