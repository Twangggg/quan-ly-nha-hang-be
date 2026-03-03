using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.KDS.Common
{
    /// <summary>
    /// Phase 2 — Priority scoring cho KDS queue.
    /// Phase 1: dùng FIFO (CreatedAt) đơn giản.
    /// </summary>
    public class KdsPriorityCalculator
    {
        /// <summary>
        /// Tính điểm ưu tiên cho món ăn. Điểm càng cao, độ ưu tiên càng lớn.
        /// </summary>
        public int Calculate(OrderItem item, Order order)
        {
            var score = 0;

            // Thời gian chờ (Wait Time): 2 điểm cho mỗi phút chờ kể từ lúc tạo
            var waitMinutes = (DateTime.UtcNow - item.CreatedAt).TotalMinutes;
            score += (int)(waitMinutes * 2);

            // Độ ưu tiên đơn hàng (VIP/Urgent): +50 điểm nếu là đơn ưu tiên
            if (order.IsPriority)
            {
                score += 50;
            }

            // Số lượng món (Quantity): Món có số lượng nhiều/phức tạp thường cần làm sớm hơn
            score += item.Quantity * 3;

            return score;
        }
    }
}
