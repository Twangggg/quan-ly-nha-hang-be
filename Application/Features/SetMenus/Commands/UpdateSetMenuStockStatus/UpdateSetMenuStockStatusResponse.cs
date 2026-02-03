namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public class UpdateSetMenuStockStatusResponse
    {
        public Guid SetMenuId { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
