using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
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
                    page.Margin(5, Unit.Millimetre);
                    page.Size(PageSizes.A5);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header()
                        .Element(headerContainer =>
                        {
                            headerContainer.Column(column =>
                            {
                                column
                                    .Item()
                                    .AlignCenter()
                                    .Text("PHIẾU TẠM TÍNH")
                                    .FontSize(14)
                                    .SemiBold();
                                column
                                    .Item()
                                    .AlignCenter()
                                    .Text("FoodHub Restaurant")
                                    .FontSize(12)
                                    .SemiBold();
                                column
                                    .Item()
                                    .AlignCenter()
                                    .Text($"Số: {data.OrderCode}")
                                    .FontSize(10)
                                    .SemiBold();
                            });
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Info section using Table for alignment
                            column
                                .Item()
                                .PaddingBottom(5)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(70);
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Text("Ngày:").SemiBold();
                                    table
                                        .Cell()
                                        .Text(
                                            data.PrintedAt.ToLocalTime()
                                                .ToString("dd/MM/yyyy (hh:mm tt)")
                                        );

                                    table.Cell().Text("Bàn:").SemiBold();
                                    table
                                        .Cell()
                                        .Text(
                                            data.TableNumber.HasValue
                                                ? $"Bàn {data.TableNumber}"
                                                : "Mang về"
                                        );

                                    table.Cell().Text("Nhân viên:").SemiBold();
                                    table.Cell().Text(data.EmployeeName);

                                    if (!string.IsNullOrEmpty(data.CustomerName))
                                    {
                                        table.Cell().Text("KH:").SemiBold();
                                        table.Cell().Text(data.CustomerName);
                                    }

                                    if (!string.IsNullOrEmpty(data.CustomerPhone))
                                    {
                                        table.Cell().Text("SĐT:").SemiBold();
                                        table.Cell().Text(data.CustomerPhone);
                                    }
                                });

                            // Items Table
                            column
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Tên món
                                        columns.RelativeColumn(1); // SL
                                        columns.RelativeColumn(2); // ĐG
                                        // columns.RelativeColumn(1);  // % KM (Omitted as no data)
                                        columns.RelativeColumn(2); // Thành tiền
                                    });

                                    table.Header(header =>
                                    {
                                        header
                                            .Cell()
                                            .BorderTop(1)
                                            .BorderBottom(1)
                                            .PaddingVertical(3)
                                            .Text("Tên món")
                                            .SemiBold();
                                        header
                                            .Cell()
                                            .BorderTop(1)
                                            .BorderBottom(1)
                                            .PaddingVertical(3)
                                            .AlignCenter()
                                            .Text("SL")
                                            .SemiBold();
                                        header
                                            .Cell()
                                            .BorderTop(1)
                                            .BorderBottom(1)
                                            .PaddingVertical(3)
                                            .AlignRight()
                                            .Text("ĐG")
                                            .SemiBold();
                                        header
                                            .Cell()
                                            .BorderTop(1)
                                            .BorderBottom(1)
                                            .PaddingVertical(3)
                                            .AlignRight()
                                            .Text("Thành tiền")
                                            .SemiBold();
                                    });

                                    for (int i = 0; i < data.Items.Count; i++)
                                    {
                                        var item = data.Items[i];

                                        table
                                            .Cell()
                                            .PaddingVertical(3)
                                            .Column(c =>
                                            {
                                                c.Item().Text(item.ItemName);
                                                if (!string.IsNullOrEmpty(item.OptionsSummary))
                                                {
                                                    c.Item()
                                                        .Text(item.OptionsSummary)
                                                        .FontSize(8)
                                                        .Italic();
                                                }
                                            });
                                        table
                                            .Cell()
                                            .PaddingVertical(3)
                                            .AlignCenter()
                                            .Text(item.Quantity.ToString());
                                        table
                                            .Cell()
                                            .PaddingVertical(3)
                                            .AlignRight()
                                            .Text(item.UnitPrice.ToString("N0"));
                                        table
                                            .Cell()
                                            .PaddingVertical(3)
                                            .AlignRight()
                                            .Text(item.LineTotal.ToString("N0"));

                                        // Dashed line except for the last item
                                        if (i < data.Items.Count - 1)
                                        {
                                            table
                                                .Cell()
                                                .ColumnSpan(4)
                                                .LineHorizontal(1)
                                                .LineColor(Colors.Black); // Simulated solid line instead of dashed asQuestPDF Line doesn't support dashed easily without canvas. Let's use a standard line but lighter.
                                            // Wait, in order to make it look nicer or dashed, we might use a text string of dashes or lighter line.
                                        }
                                    }
                                    // Bottom border of the table items
                                    table.Cell().ColumnSpan(4).BorderBottom(1).PaddingTop(2);
                                });

                            // Summary section
                            column
                                .Item()
                                .PaddingTop(10)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(100);
                                    });

                                    table.Cell().PaddingVertical(2).Text("Tiền hàng").Italic();
                                    table
                                        .Cell()
                                        .PaddingVertical(2)
                                        .AlignRight()
                                        .Text($"{data.SubTotal:N0}đ")
                                        .Italic();

                                    if (data.Discount > 0)
                                    {
                                        table.Cell().PaddingVertical(2).Text("Giảm giá").Italic();
                                        table
                                            .Cell()
                                            .PaddingVertical(2)
                                            .AlignRight()
                                            .Text($"-{data.Discount:N0}đ")
                                            .Italic();
                                    }

                                    table
                                        .Cell()
                                        .PaddingVertical(2)
                                        .Text("Tiền trước thuế")
                                        .SemiBold();
                                    table
                                        .Cell()
                                        .PaddingVertical(2)
                                        .AlignRight()
                                        .Text($"{data.PreTaxAmount:N0}đ")
                                        .SemiBold();

                                    table
                                        .Cell()
                                        .PaddingVertical(2)
                                        .Text($"Thuế GTGT ({data.VatRate:0}%)")
                                        .Italic();
                                    table
                                        .Cell()
                                        .PaddingVertical(2)
                                        .AlignRight()
                                        .Text($"{data.Vat:N0}đ")
                                        .Italic();

                                    table
                                        .Cell()
                                        .PaddingVertical(4)
                                        .Text("Tổng thanh toán")
                                        .FontSize(12)
                                        .SemiBold();
                                    table
                                        .Cell()
                                        .PaddingVertical(4)
                                        .AlignRight()
                                        .Text($"{data.TotalAmount:N0}đ")
                                        .FontSize(12)
                                        .SemiBold();
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().PaddingTop(10).LineHorizontal(1);
                            column
                                .Item()
                                .PaddingTop(5)
                                .Text("Đây không phải là hóa đơn thanh toán")
                                .Italic()
                                .FontSize(9);
                            column.Item().Text("Cảm ơn quý khách và hẹn gặp lại!").FontSize(9);
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
