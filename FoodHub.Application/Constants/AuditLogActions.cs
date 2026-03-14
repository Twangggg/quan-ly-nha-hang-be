namespace FoodHub.Application.Constants
{
    public static class AuditLogActions
    {
        public const string CreateOrder = "CREATE_ORDER";
        public const string SubmitOrder = "SUBMIT_ORDER";
        public const string AddOrderItem = "ADD_ORDER_ITEM";
        public const string UpdateOrderItem = "UPDATE_ORDER_ITEM";
        public const string AdjustOrderItemQuantity = "ADJUST_ORDER_ITEM_QUANTITY";
        public const string CancelOrderItem = "CANCEL_ORDER_ITEM";
        public const string CancelOrder = "CANCEL_ORDER";
        public const string CompleteOrder = "COMPLETE_ORDER";
        public const string CheckoutOrder = "CHECKOUT_ORDER";
        public const string KdsStartCooking = "KDS_START_COOKING";
        public const string KdsMarkReady = "KDS_MARK_READY";
        public const string KdsReject = "KDS_REJECT";
        public const string KdsReturn = "KDS_RETURN";
        public const string CheckInReservation = "CHECK_IN_RESERVATION";
    }
}
