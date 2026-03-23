using System;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    /// <summary>
    /// Returns the order and table snapshot after a successful table change.
    /// </summary>
    public class ChangeOrderTableResponse
    {
        /// <summary>
        /// Identifier of the moved order.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Code of the moved order.
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the previous table.
        /// </summary>
        public Guid OldTableId { get; set; }

        /// <summary>
        /// Display name of the previous table.
        /// </summary>
        public string OldTableName { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the new table.
        /// </summary>
        public Guid NewTableId { get; set; }

        /// <summary>
        /// Display name of the new table.
        /// </summary>
        public string NewTableName { get; set; } = string.Empty;
    }
}
