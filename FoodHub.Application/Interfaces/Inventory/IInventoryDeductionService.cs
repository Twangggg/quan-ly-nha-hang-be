namespace FoodHub.Application.Interfaces.Inventory
{
    /// <summary>
    /// Service for handling inventory stock deduction based on menu item recipes.
    /// </summary>
    public interface IInventoryDeductionService
    {
        /// <summary>
        /// Deducts stock for all items in an order based on their recipes.
        /// </summary>
        /// <param name="orderId">The ID of the order to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeductStockAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deducts stock for a single order item when it's marked as Ready.
        /// </summary>
        /// <param name="orderItemId">The ID of the order item.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeductStockForItemAsync(
            Guid orderItemId,
            CancellationToken cancellationToken = default
        );
    }
}
