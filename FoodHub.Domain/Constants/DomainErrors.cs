namespace FoodHub.Domain.Constants
{
    public static class DomainErrors
    {
        public static class Order
        {
            public const string InvalidStatusForCancel = "Order.InvalidStatusForCancel";
            public const string OrderNotReadyForCompletion = "Order.OrderNotReadyForCompletion";
            public const string NotFound = "Order.NotFound";
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
    }
}
