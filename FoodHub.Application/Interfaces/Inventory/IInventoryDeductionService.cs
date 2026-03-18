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
    }
}
