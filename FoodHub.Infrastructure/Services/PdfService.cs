using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodHub.Infrastructure.Services
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

                    page.Header().Element(headerContainer =>
                    {
                        headerContainer.Column(column =>
                        {
                            column.Item().AlignCenter().Text("PHIẾU TẠM TÍNH").FontSize(14).SemiBold();
                            column.Item().AlignCenter().Text("FoodHub Restaurant").FontSize(12).SemiBold();
                            column.Item().AlignCenter().Text($"Số: {data.OrderCode}").FontSize(10).SemiBold();
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Info section using Table for alignment
                        column.Item().PaddingBottom(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Ngày:").SemiBold();
                            table.Cell().Text(data.PrintedAt.ToLocalTime().ToString("dd/MM/yyyy (hh:mm tt)"));

                            table.Cell().Text("Bàn:").SemiBold();
                            table.Cell().Text(data.TableNumber.HasValue ? $"Bàn {data.TableNumber}" : "Mang về");

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
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);  // Tên món
                                columns.RelativeColumn(1);  // SL
                                columns.RelativeColumn(2);  // ĐG
                                // columns.RelativeColumn(1);  // % KM (Omitted as no data)
                                columns.RelativeColumn(2);  // Thành tiền
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).Text("Tên món").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignCenter().Text("SL").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignRight().Text("ĐG").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignRight().Text("Thành tiền").SemiBold();
                            });

                            for (int i = 0; i < data.Items.Count; i++)
                            {
                                var item = data.Items[i];

                                table.Cell().PaddingVertical(3).Column(c =>
                                {
                                    c.Item().Text(item.ItemName);
                                    if (!string.IsNullOrEmpty(item.OptionsSummary))
                                    {
                                        c.Item().Text(item.OptionsSummary).FontSize(8).Italic();
                                    }
                                });
                                table.Cell().PaddingVertical(3).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().PaddingVertical(3).AlignRight().Text(item.UnitPrice.ToString("N0"));
                                table.Cell().PaddingVertical(3).AlignRight().Text(item.LineTotal.ToString("N0"));

                                // Dashed line except for the last item
                                if (i < data.Items.Count - 1)
                                {
                                    table.Cell().ColumnSpan(4).LineHorizontal(1).LineColor(Colors.Black); // Simulated solid line instead of dashed asQuestPDF Line doesn't support dashed easily without canvas. Let's use a standard line but lighter. 
                                    // Wait, in order to make it look nicer or dashed, we might use a text string of dashes or lighter line.
                                }
                            }
                            // Bottom border of the table items
                            table.Cell().ColumnSpan(4).BorderBottom(1).PaddingTop(2);
                        });

                        // Summary section
                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Cell().PaddingVertical(2).Text("Tiền hàng").Italic();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{data.SubTotal:N0}đ").Italic();

                            if (data.Discount > 0)
                            {
                                table.Cell().PaddingVertical(2).Text("Giảm giá").Italic();
                                table.Cell().PaddingVertical(2).AlignRight().Text($"-{data.Discount:N0}đ").Italic();
                            }

                            table.Cell().PaddingVertical(2).Text("Tiền trước thuế").SemiBold();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{data.PreTaxAmount:N0}đ").SemiBold();

                            table.Cell().PaddingVertical(2).Text($"Thuế GTGT ({data.VatRate:0}%)").Italic();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{data.Vat:N0}đ").Italic();

                            table.Cell().PaddingVertical(4).Text("Tổng thanh toán").FontSize(12).SemiBold();
                            table.Cell().PaddingVertical(4).AlignRight().Text($"{data.TotalAmount:N0}đ").FontSize(12).SemiBold();
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

        public byte[] GenerateInvoicePdf(Invoice invoice)
        {
            var document = Document.Create(container =>
            {
                // Format khổ giấy in nhiệt (Roll80)
                container.Page(page =>
                {
                    // Chiều rộng cuộn giấy 80mm (~3.14 inch)
                    page.Margin(5, Unit.Millimetre);
                    page.ContinuousSize(3.14f, Unit.Inch);

                    // Cài font mặc định chữ nhỏ để vừa khổ giấy con
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    // ==============================
                    // HEADER: THÔNG TIN NHÀ HÀNG
                    // ==============================
                    page.Header().Element(headerContainer =>
                    {
                        headerContainer.Column(column =>
                        {
                            column.Item().AlignCenter().Text("FOODHUB RESTAURANT")
                                  .FontSize(14).SemiBold();
                            column.Item().AlignCenter().Text("123 Đường Ẩm Thực, Quận 1, TP.HCM");
                            column.Item().AlignCenter().Text("Tel: 0909 123 456");

                            // Tiêu đề
                            column.Item().PaddingTop(10).AlignCenter().Text("HÓA ĐƠN THANH TOÁN")
                                  .FontSize(12).SemiBold();

                            // Chữ đường đứt nét
                            column.Item().PaddingTop(5).Text("-----------------------------------------------------------------")
                                  .AlignCenter().FontSize(8);
                        });
                    });

                    // ==============================
                    // CONTENT: CHI TIẾT HÓA ĐƠN
                    // ==============================
                    page.Content().PaddingVertical(5).Column(column =>
                    {
                        // 1. Thông tin chung (Số HĐ, Ngày, Thu Ngân...)
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Số HĐ: {invoice.InvoiceNumber}");
                            row.RelativeItem().AlignRight().Text($"Bàn: {invoice.TableNumber}"); // Hoặc dùng tên bàn (Cần truyền vào DTO)
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Ngày: {invoice.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Thu ngân: {invoice.CashierName}");
                        });

                        column.Item().PaddingVertical(2).Text("-----------------------------------------------------------------").AlignCenter().FontSize(8);

                        // 2. Bảng chi tiết món ăn (Header: Tên món | SL | Đơn giá | T.Tiền)
                        column.Item().Table(table =>
                        {
                            // Định nghĩa cột (Tỉ lệ độ rộng)
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Tên món
                                columns.RelativeColumn(1); // SL
                                columns.RelativeColumn(2); // ĐG
                                columns.RelativeColumn(3); // Tổng tiền
                            });

                            // Loop danh sách món
                            foreach (var item in invoice.Items)
                            {
                                // Tên món (Dòng trên)
                                table.Cell().ColumnSpan(4).Text($"{item.ItemName}")
                                     .FontSize(9).SemiBold();

                                // Nếu có Note
                                if (!string.IsNullOrEmpty(item.Note))
                                {
                                    table.Cell().ColumnSpan(4).Text($"({item.Note})")
                                         .FontSize(8).Italic();
                                }

                                // SL, Giá, Thành tiền (Dòng dưới)
                                table.Cell().Text(""); // Empty cell for spacing under name
                                table.Cell().AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().AlignRight().Text(item.UnitPrice.ToString("N0"));
                                table.Cell().AlignRight().Text(item.TotalPrice.ToString("N0"));
                            }
                        });

                        column.Item().PaddingVertical(2).Text("-----------------------------------------------------------------").AlignCenter().FontSize(8);

                        // 3. Phần tổng hợp tiền (Summary)
                        column.Item().Column(c =>
                        {
                            c.Item().Row(row => {
                                row.RelativeItem().Text("Tổng cộng:").SemiBold();
                                row.RelativeItem().AlignRight().Text($"{invoice.SubTotal:N0} đ");
                            });

                            if (invoice.TaxAmount > 0)
                            {
                                c.Item().Row(row => {
                                    row.RelativeItem().Text("Thuế (VAT):");
                                    row.RelativeItem().AlignRight().Text($"{invoice.TaxAmount:N0} đ");
                                });
                            }

                            if (invoice.DiscountAmount > 0)
                            {
                                c.Item().Row(row => {
                                    row.RelativeItem().Text("Giảm giá:");
                                    row.RelativeItem().AlignRight().Text($"-{invoice.DiscountAmount:N0} đ");
                                });
                            }

                            c.Item().PaddingVertical(2).Text("-----------------------------------------------------------------").AlignCenter().FontSize(8);

                            // KHÁCH PHẢI TRẢ (In đậm to)
                            c.Item().Row(row => {
                                row.RelativeItem().Text("KHÁCH PHẢI TRẢ:").SemiBold().FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{invoice.TotalAmount:N0} đ").SemiBold().FontSize(10);
                            });

                            // Tiền Đưa & Trả Lại
                            c.Item().PaddingTop(5).Row(row => {
                                row.RelativeItem().Text($"Tiền mặt/Chuyển khoản:");
                                row.RelativeItem().AlignRight().Text($"{invoice.AmountReceived:N0} đ");
                            });

                            c.Item().Row(row => {
                                row.RelativeItem().Text("Tiền thừa trả khách:");
                                row.RelativeItem().AlignRight().Text($"{invoice.AmountReturned:N0} đ");
                            });
                        });
                    });

                    // ==============================
                    // FOOTER: LỜI CẢM ƠN
                    // ==============================
                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().PaddingTop(10).AlignCenter().Text("CẢM ƠN QUÝ KHÁCH HẸN GẶP LẠI!").SemiBold();
                        column.Item().AlignCenter().Text("Powered by FoodHub System").FontSize(8).Italic();
                        column.Item().PaddingBottom(10).Text(""); // Trống ở cuối để dễ xé giấy
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
