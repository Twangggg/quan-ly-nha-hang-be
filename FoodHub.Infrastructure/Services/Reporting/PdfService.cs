using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace FoodHub.Infrastructure.Services.Reporting
{
    public class PdfService : IPdfService
    {
        public byte[] GeneratePreCheckBill(GetPreCheckBillResponse data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(1, Unit.Centimetre);
                    page.Size(PageSizes.A5);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Element(headerContainer =>
                    {
                        headerContainer.Column(column =>
                        {
                            column.Item().AlignCenter().Text("FOODHUB RESTAURANT").FontSize(16).SemiBold();
                            column.Item().AlignCenter().Text("PHIẾU TẠM TÍNH").FontSize(14).SemiBold();
                            column.Item().PaddingTop(5).LineHorizontal(1);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Info section
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Bàn: {data.TableNumber}");
                            row.RelativeItem().AlignRight().Text($"Mã đơn: {data.OrderCode}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"NV phục vụ: {data.EmployeeName}");
                            row.RelativeItem().AlignRight().Text($"Giờ in: {data.PrintedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
                        });

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20); // No.
                                columns.RelativeColumn();   // Item Name
                                columns.ConstantColumn(30); // Qty
                                columns.ConstantColumn(70); // Price
                                columns.ConstantColumn(80); // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#").SemiBold();
                                header.Cell().Text("Món ăn").SemiBold();
                                header.Cell().AlignCenter().Text("SL").SemiBold();
                                header.Cell().AlignRight().Text("Đơn giá").SemiBold();
                                header.Cell().AlignRight().Text("Thành tiền").SemiBold();
                            });

                            for (int i = 0; i < data.Items.Count; i++)
                            {
                                var item = data.Items[i];
                                table.Cell().Text((i + 1).ToString());
                                table.Cell().Column(c =>
                                {
                                    c.Item().Text(item.ItemName);
                                    if (!string.IsNullOrEmpty(item.OptionsSummary))
                                    {
                                        c.Item().Text(item.OptionsSummary).FontSize(8).Italic();
                                    }
                                });
                                table.Cell().AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().AlignRight().Text(item.UnitPrice.ToString("N0"));
                                table.Cell().AlignRight().Text(item.LineTotal.ToString("N0"));
                            }
                        });

                        // Summary section
                        column.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Tạm tính: {data.SubTotal:N0} VNĐ");
                            if (data.Discount > 0)
                                c.Item().Text($"Giảm giá: -{data.Discount:N0} VNĐ");
                            if (data.Vat > 0)
                                c.Item().Text($"VAT: {data.Vat:N0} VNĐ");

                            c.Item().PaddingTop(5).Text($"TỔNG CỘNG: {data.TotalAmount:N0} VNĐ").FontSize(12).SemiBold();
                        });
                    });

                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().PaddingTop(10).LineHorizontal(1);
                        column.Item().PaddingTop(5).Text("Đây không phải là hóa đơn thanh toán").Italic().FontSize(9);
                        column.Item().Text("Cảm ơn quý khách và hẹn gặp lại!").FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
