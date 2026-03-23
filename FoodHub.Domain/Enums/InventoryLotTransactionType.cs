namespace FoodHub.Domain.Enums
{
    public enum InventoryLotTransactionType
    {
        StockIn = 1,
        StockInReverse = 2,
        StockOut = 3,
        StockOutReverse = 4,
        SaleDeduction = 5,
        SaleDeductionReverse = 6,
        InventoryCheck = 7,
        Dispose = 8,
        Adjustment = 9,
    }
}
