using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    /// <summary>
    /// Returns the source and destination order snapshots after a split or move operation.
    /// </summary>
    public class SplitOrderResponse
    {
        /// <summary>
        /// Source order identifier after the transfer.
        /// </summary>
        public Guid SourceOrderId { get; set; }
        /// <summary>
        /// Source order code after the transfer.
        /// </summary>
        public string SourceOrderCode { get; set; } = null!;
        /// <summary>
        /// Source order total after the transfer.
        /// </summary>
        public decimal SourceOrderTotalAmount { get; set; }
        /// <summary>
        /// Remaining source order items after the transfer.
        /// </summary>
        public List<SplitOrderItemDto> SourceOrderItems { get; set; } = new List<SplitOrderItemDto>();

        /// <summary>
        /// Destination order identifier after the transfer.
        /// </summary>
        public Guid DestinationOrderId { get; set; }
        /// <summary>
        /// Destination order code after the transfer.
        /// </summary>
        public string DestinationOrderCode { get; set; } = null!;
        /// <summary>
        /// Destination order total after the transfer.
        /// </summary>
        public decimal DestinationOrderTotalAmount { get; set; }
        /// <summary>
        /// Destination order items after the transfer.
        /// </summary>
        public List<SplitOrderItemDto> DestinationOrderItems { get; set; } = new List<SplitOrderItemDto>();
        /// <summary>
        /// Destination table identifier.
        /// </summary>
        public Guid? DestinationTableId { get; set; }
        /// <summary>
        /// Indicates whether a new destination order was created during the operation.
        /// </summary>
        public bool CreatedNewOrder { get; set; }
    }

    /// <summary>
    /// Lightweight order item snapshot returned by split responses.
    /// </summary>
    public class SplitOrderItemDto : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<OrderItem, SplitOrderItemDto>();
        }
    }
}
