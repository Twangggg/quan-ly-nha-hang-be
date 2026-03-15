namespace FoodHub.Domain.Constants
{
    public static class DomainErrors
    {
        public static class Order
        {
            public const string InvalidStatusForCancel = "Order.InvalidStatusForCancel";
            public const string OrderNotReadyForCompletion = "Order.OrderNotReadyForCompletion";
            public const string InvalidStatusForCheckout = "Order.InvalidStatusForCheckout";
            public const string InsufficientAmount = "Order.InsufficientAmount";
            public const string NotFound = "Order.NotFound";
            public const string InvalidActionWithStatus = "Order.InvalidActionWithStatus";
            public const string ItemsNotFinished = "Order.ItemsNotFinished";
        }

        public static class OrderItem
        {
            public const string InvalidQuantity = "OrderItem.InvalidQuantity";
            public const string InvalidStatusForCancel = "OrderItem.InvalidStatusForCancel";

            // KDS state transitions
            public const string MustBePreparingToStartCooking =
                "OrderItem.MustBePreparingToStartCooking";
            public const string MustBeCookingToReady = "OrderItem.MustBeCookingToReady";
            public const string MustBeCookingToReject = "OrderItem.MustBeCookingToReject";
            public const string RejectionReasonIsRequired = "OrderItem.RejectionReasonIsRequired";
            public const string MustBeRejectedToReturn = "OrderItem.MustBeRejectedToReturn";
        }

        public static class Category
        {
            public const string CannotDeleteActiveCategory = "Category.CannotDeleteActiveCategory";
            public const string CannotDeactivateWithActiveItems =
                "Category.CannotDeactivateWithActiveItems";
            public const string NotFound = "Category.NotFound";
        }

        public static class Area
        {
            public const string AlreadyInactive = "Area.AlreadyInactive";
        }

        public static class SetMenu
        {
            public const string NotFound = "SetMenu.NotFound";
            public const string CannotDeleteWithItems = "SetMenu.CannotDeleteWithItems";
            public const string InvalidPrice = "SetMenu.InvalidPrice";
        }

        public static class OptionGroup
        {
            public const string CannotDeleteWithOptions = "OptionGroup.CannotDeleteWithOptions";
            public const string CannotHaveBothMinAndMax = "OptionGroup.CannotHaveBothMinAndMax";
        }

        public static class OptionItem
        {
            public const string InvalidExtraPrice = "OptionItem.InvalidExtraPrice";
        }

        public static class Invoice
        {
            public const string AlreadyPaid = "Invoice.AlreadyPaid";
            public const string AlreadyCancelled = "Invoice.AlreadyCancelled";
            public const string CannotCancelPaid = "Invoice.CannotCancelPaid";
            public const string NotFound = "Invoice.NotFound";
            public const string OrderNotCompleted = "Invoice.OrderNotCompleted";
            public const string AlreadyExists = "Invoice.AlreadyExists";
        }

        public static class Payment
        {
            public const string NotFound = "Payment.NotFound";
            public const string InvalidInvoiceStatus = "Payment.InvalidInvoiceStatus";
            public const string AmountExceedsRemaining = "Payment.AmountExceedsRemaining";
        }

        public static class Ingredient
        {
            public const string NotFound = "Ingredient.NotFound";
            public const string DuplicateCode = "Ingredient.DuplicateCode";
            public const string DuplicateName = "Ingredient.DuplicateName";
            public const string UsedInRecipe = "Ingredient.UsedInRecipe";
            public const string InsufficientStock = "Ingredient.InsufficientStock";
            public const string InvalidStockInQuantity = "Ingredient.InvalidStockInQuantity";
            public const string InvalidStockInCost = "Ingredient.InvalidStockInCost";
            public const string InvalidOpeningStockQuantity =
                "Ingredient.InvalidOpeningStockQuantity";
            public const string InvalidOpeningStockCost = "Ingredient.InvalidOpeningStockCost";
        }

        public static class InventorySettings
        {
            public const string InvalidExpiryWarningDays =
                "InventorySettings.InvalidExpiryWarningDays";
            public const string InvalidLowStockThreshold =
                "InventorySettings.InvalidLowStockThreshold";
            public const string InvalidMaxCostRecalcDays =
                "InventorySettings.InvalidMaxCostRecalcDays";
        }

        public static class StockInReceipt
        {
            public const string DuplicateIngredient = "StockInReceipt.DuplicateIngredient";
            public const string InvalidQuantity = "StockInReceipt.InvalidQuantity";
            public const string InvalidUnitCost = "StockInReceipt.InvalidUnitCost";
            public const string AlreadyReversed = "StockInReceipt.AlreadyReversed";
        }
    }
}
