using System;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    /// <summary>
    /// Returns the order and table snapshot after a successful table change.
    /// </summary>
    public class ChangeOrderTableResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public Guid OldTableId {  get; set; }
        public string OldTableName { get; set; } = string.Empty;
        public Guid NewTableId { get; set; }
        public string NewTableName { get; set; } = string.Empty;
    }
}
