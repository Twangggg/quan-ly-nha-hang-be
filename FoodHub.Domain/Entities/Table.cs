using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Table : BaseEntity
    {
        public Guid TableId { get; set; }
        public int TableNumber { get; set; }
        public int Capacity { get; set; }
        public Guid AreaId { get; set; }
        public virtual Area Area { get; set; } = null!;
        public TableStatus Status { get; set; } = TableStatus.Available;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public void MarkAsCleaning()
        {
            Status = TableStatus.Cleaning;
        }
        public void MarkAsAvailable()
        {
            Status = TableStatus.Available;
        }

        public string GetTableName()
        {
            if (Area != null && !string.IsNullOrEmpty(Area.CodePrefix))
            {
                return $"{Area.CodePrefix}_{TableNumber}";
            }

            return TableNumber.ToString();
        }

        /// <summary>
        /// Dùng để chuyển trạng thái bàn về Available sau khi đã kiểm tra điều kiện (không có order nào đang phục vụ)
        /// Bắt buộc phải include Orders và kiểm tra trạng thái của các order trước khi gọi phương thức này để đảm bảo tính đúng đắn của trạng thái bàn
        /// </summary>
        /// <returns>
        /// true nếu bàn đã được chuyển về trạng thái Available thành công, false nếu không thể chuyển do có order đang phục vụ
        /// </returns>
        public bool SetAvailable()
        {
            if (!CanAvailable())
            {
                return false; // Cannot set to available if there are active orders
            }

            Status = TableStatus.Available;
            return true;
        }

        public bool CanAvailable()
        {
            if (Orders.Any(o => o.Status == OrderStatus.Serving))
            {
                return false;
            }

            return true;
        }
    }
}
