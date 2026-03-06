using System;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Common
{
    /// <summary>
    /// Phase 2 — Priority scoring cho KDS queue.
    /// Phase 1: dùng FIFO (CreatedAt) đơn giản.
    /// </summary>
    public class KdsPriorityCalculator
    {
        /// <summary>
        /// Tính điểm ưu tiên nâng cao cho món ăn (Phase 2 - Smart Heuristics).
        /// </summary>
        public int Calculate(
            DateTime createdAt,
            bool isOrderPriority,
            int expectedTimeSeconds,
            OrderType orderType,
            int totalOrderItems,
            int finishedOrderItems
        )
        {
            var score = 0.0;
            var now = DateTime.UtcNow;

            // Base Wait Time: +2 pts per minute
            var waitMinutes = (now - createdAt).TotalMinutes;
            score += waitMinutes * 2;

            // Order Priority: +100 pts
            if (isOrderPriority)
            {
                score += 100;
            }

            // Preparation Type Weight
            var expectedTimeMinutes = expectedTimeSeconds / 60.0;
            score += expectedTimeMinutes * 1.5;

            // Stale Item Penalty: +10 pts per minute AFTER ExpectedTime
            if (waitMinutes > expectedTimeMinutes)
            {
                var overdueMinutes = waitMinutes - expectedTimeMinutes;
                score += overdueMinutes * 10;
            }

            // Order Completion Boost
            if (totalOrderItems > 0)
            {
                score += ((double)finishedOrderItems / totalOrderItems) * 50;
            }

            // Order Type Factor
            score += orderType switch
            {
                OrderType.Takeaway => 15,
                OrderType.Delivery => 25,
                _ => 0,
            };

            return (int)Math.Round(score);
        }
    }
}
