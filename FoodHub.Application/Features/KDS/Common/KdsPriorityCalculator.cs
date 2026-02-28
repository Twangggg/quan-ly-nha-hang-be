using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.KDS.Common
{
    /// <summary>
    /// Phase 2 — Priority scoring cho KDS queue.
    /// Phase 1: dùng FIFO (CreatedAt) đơn giản.
    /// </summary>
    public class KdsPriorityCalculator
    {
        // TODO: Implement priority scoring
        // Score dựa trên: waitTime + IsPriority (VIP) + Quantity
        public int Calculate(OrderItem item, Order order)
        {
            throw new NotImplementedException();
        }
    }
}
