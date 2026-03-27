using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces.Kds
{
    public interface IKdsAutoPullService
    {
        /// <summary>
        /// Calculates available slots for each station.
        /// </summary>
        Task<Dictionary<string, int>> GetAvailableSlotsAsync(
            IEnumerable<string> stations,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Attempts to pull the next priority item(s) into cooking if slots are available.
        /// This is used when an item is completed or rejected.
        /// Returns the list of items that were moved to cooking.
        /// </summary>
        Task<List<FoodHub.Domain.Entities.OrderItem>> ProcessAutoPullAsync(
            string station,
            Guid employeeId,
            CancellationToken cancellationToken
        );
    }
}
