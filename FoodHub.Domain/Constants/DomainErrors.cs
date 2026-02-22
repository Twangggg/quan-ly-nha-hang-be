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
        }
    }
}
