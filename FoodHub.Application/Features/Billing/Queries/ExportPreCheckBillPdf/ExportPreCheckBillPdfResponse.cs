namespace FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf
{
    /// <summary>
    /// Dữ liệu file PDF của phiếu tạm tính.
    /// </summary>
    public class ExportPreCheckBillPdfResponse
    {
        /// <summary>
        /// Nội dung file PDF dạng nhị phân.
        /// </summary>
        public byte[] Content { get; set; } = default!;

        /// <summary>
        /// Tên file PDF sẽ trả về cho client.
        /// </summary>
        public string FileName { get; set; } = default!;
    }
}
