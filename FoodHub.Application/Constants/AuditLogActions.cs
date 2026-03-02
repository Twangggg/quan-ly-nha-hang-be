namespace FoodHub.Application.Constants
{
    public static class AuditLogActions
    {
        public const string SubmitOrder = "SUBMIT_ORDER";
        public const string AddOrderItem = "ADD_ORDER_ITEM";
        public const string UpdateOrderItem = "UPDATE_ORDER_ITEM";
        public const string CancelOrderItem = "CANCEL_ORDER_ITEM";
        public const string CancelOrder = "CANCEL_ORDER";
        public const string CompleteOrder = "COMPLETE_ORDER";

        // KDS Actions
        public const string KdsStartCooking = "KDS_START_COOKING";
        public const string KdsMarkReady = "KDS_MARK_READY";
        public const string KdsReject = "KDS_REJECT";
        public const string KdsReturn = "KDS_RETURN";
    }
}
