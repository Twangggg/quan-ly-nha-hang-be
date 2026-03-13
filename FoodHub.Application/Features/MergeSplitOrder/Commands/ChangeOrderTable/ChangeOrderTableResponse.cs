using System;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    public class ChangeOrderTableResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public Guid OldTableId {  get; set; }
        public string OldTableName { get; set; }
        public Guid NewTableId { get; set; }
        public string NewTableName { get; set; }
    }
}
