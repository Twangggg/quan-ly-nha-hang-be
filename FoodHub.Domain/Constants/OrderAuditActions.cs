namespace FoodHub.Domain.Constants
{
    public static class OrderAuditActions
    {
        public const string CreateOrder = "CREATE_ORDER";
        public const string SubmitOrder = "SUBMIT_ORDER";
        public const string AddOrderItem = "ADD_ORDER_ITEM";
        public const string UpdateOrderItem = "UPDATE_ORDER_ITEM";
        public const string CancelOrderItem = "CANCEL_ORDER_ITEM";
        public const string CancelOrder = "CANCEL_ORDER";
        public const string CompleteOrder = "COMPLETE_ORDER";
        public const string MergeOrder = "MERGE_ORDER";
        public const string SplitOrder = "SPLIT_ORDER";
        public const string SplitBill = "SPLIT_BILL";
        public const string ChangeOrderTable = "CHANGE_ORDER_TABLE";
        public const string CheckoutOrder = "CHECKOUT_ORDER";
        public const string KdsStartCooking = "KDS_START_COOKING";
        public const string KdsCompleteCooking = "KDS_COMPLETE_COOKING";
        public const string KdsReject = "KDS_REJECT";
        public const string KdsReturn = "KDS_RETURN";
        public const string CheckInReservation = "CHECK_IN_RESERVATION";
        public const string AdjustOrderItemQuantity = "ADJUST_ORDER_ITEM_QUANTITY";
    }
}
