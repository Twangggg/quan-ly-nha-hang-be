namespace FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf
{
    public class ExportPreCheckBillPdfResponse
    {
        public byte[] Content { get; set; } = default!;
        public string FileName { get; set; } = default!;
    }
}
