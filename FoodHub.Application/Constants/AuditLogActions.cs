using FoodHub.Domain.Constants;

namespace FoodHub.Application.Constants
{
    public static class AuditLogActions
    {
        public const string CreateOrder = OrderAuditActions.CreateOrder;
        public const string SubmitOrder = OrderAuditActions.SubmitOrder;
        public const string AddOrderItem = OrderAuditActions.AddOrderItem;
        public const string UpdateOrderItem = OrderAuditActions.UpdateOrderItem;
        public const string CancelOrderItem = OrderAuditActions.CancelOrderItem;
        public const string CancelOrder = OrderAuditActions.CancelOrder;
        public const string CompleteOrder = OrderAuditActions.CompleteOrder;
        public const string CheckoutOrder = OrderAuditActions.CheckoutOrder;
        public const string KdsStartCooking = OrderAuditActions.KdsStartCooking;
        public const string KdsMarkReady = OrderAuditActions.KdsMarkReady;
        public const string KdsReject = OrderAuditActions.KdsReject;
        public const string KdsReturn = OrderAuditActions.KdsReturn;
        public const string CheckInReservation = OrderAuditActions.CheckInReservation;
    }
}
