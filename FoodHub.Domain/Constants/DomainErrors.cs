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
    }
}
