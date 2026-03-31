using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces.Branding;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodHub.Infrastructure.Services.Reporting
{
    public class PdfService : IPdfService
    {
        private readonly IBrandingSettingsProvider _brandingSettingsProvider;
        private readonly IBrandingFormatter _brandingFormatter;

        public PdfService(
            IBrandingSettingsProvider brandingSettingsProvider,
            IBrandingFormatter brandingFormatter
        )
        {
            _brandingSettingsProvider = brandingSettingsProvider;
            _brandingFormatter = brandingFormatter;
        }

        public byte[] GeneratePreCheckBill(GetPreCheckBillResponse data)
        {
            var branding = _brandingSettingsProvider.GetOrCreateAsync().GetAwaiter().GetResult();
            var printedAt = _brandingFormatter.FormatDateTime(data.PrintedAt);

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
                            column.Item().AlignCenter().Text(branding.BillTitle).FontSize(14).SemiBold();
                            column.Item().AlignCenter().Text(branding.RestaurantName).FontSize(12).SemiBold();
                            column.Item().AlignCenter().Text($"So: {data.OrderCode}").FontSize(10).SemiBold();
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().PaddingBottom(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Ngay:").SemiBold();
                            table.Cell().Text(printedAt);

                            table.Cell().Text("Ban:").SemiBold();
                            table.Cell()
                                .Text(data.TableNumber.HasValue ? $"Ban {data.TableNumber}" : "Mang ve");

                            table.Cell().Text("Nhan vien:").SemiBold();
                            table.Cell().Text(data.EmployeeName);

                            if (!string.IsNullOrEmpty(data.CustomerName))
                            {
                                table.Cell().Text("KH:").SemiBold();
                                table.Cell().Text(data.CustomerName);
                            }

                            if (!string.IsNullOrEmpty(data.CustomerPhone))
                            {
                                table.Cell().Text("SDT:").SemiBold();
                                table.Cell().Text(data.CustomerPhone);
                            }
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).Text("Ten mon").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignCenter().Text("SL").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignRight().Text("DG").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignRight().Text("Thanh tien").SemiBold();
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

                                if (i < data.Items.Count - 1)
                                {
                                    table.Cell().ColumnSpan(4).LineHorizontal(1).LineColor(Colors.Black);
                                }
                            }

                            table.Cell().ColumnSpan(4).BorderBottom(1).PaddingTop(2);
                        });

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Cell().PaddingVertical(2).Text("Tien hang").Italic();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{data.SubTotal:N0}d").Italic();

                            if (data.Discount > 0)
                            {
                                table.Cell().PaddingVertical(2).Text("Giam gia").Italic();
                                table.Cell().PaddingVertical(2).AlignRight().Text($"-{data.Discount:N0}d").Italic();
                            }

                            table.Cell().PaddingVertical(2).Text("Tien truoc thue").SemiBold();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{data.PreTaxAmount:N0}d").SemiBold();

                            table.Cell().PaddingVertical(2).Text($"Thue GTGT ({data.VatRate:0}%)").Italic();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{data.Vat:N0}d").Italic();

                            table.Cell().PaddingVertical(4).Text("Tong thanh toan").FontSize(12).SemiBold();
                            table.Cell().PaddingVertical(4).AlignRight().Text($"{data.TotalAmount:N0}d").FontSize(12).SemiBold();
                        });
                    });

                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().PaddingTop(10).LineHorizontal(1);
                        column.Item().PaddingTop(5).Text(branding.BillFooter).Italic().FontSize(9);
                        if (!string.IsNullOrWhiteSpace(branding.Address))
                        {
                            column.Item().Text(branding.Address).FontSize(9);
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateInvoicePdf(Invoice invoice)
        {
            var branding = _brandingSettingsProvider.GetOrCreateAsync().GetAwaiter().GetResult();
            var printedAt = _brandingFormatter.FormatDateTime(invoice.CreatedAt);

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
                            column.Item().AlignCenter().Text("Hoa don thanh toan").FontSize(14).SemiBold();
                            column.Item().AlignCenter().Text(branding.RestaurantName).FontSize(12).SemiBold();
                            column.Item().AlignCenter().Text($"So HD: {invoice.InvoiceNumber}").FontSize(10).SemiBold();
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().PaddingBottom(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Ngay:").SemiBold();
                            table.Cell().Text(printedAt);

                            table.Cell().Text("Ban:").SemiBold();
                            table.Cell()
                                .Text(!string.IsNullOrEmpty(invoice.TableNumber) ? $"Ban {invoice.TableNumber}" : "Mang ve");

                            table.Cell().Text("Thu ngan:").SemiBold();
                            table.Cell().Text(invoice.CashierName);

                            table.Cell().Text("Hinh thuc:").SemiBold();
                            table.Cell().Text(invoice.PaymentMethod.ToString());
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).Text("Ten mon").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignCenter().Text("SL").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignRight().Text("DG").SemiBold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(3).AlignRight().Text("Thanh tien").SemiBold();
                            });

                            var itemsList = invoice.Items.ToList();
                            for (int i = 0; i < itemsList.Count; i++)
                            {
                                var item = itemsList[i];

                                table.Cell().PaddingVertical(3).Column(c =>
                                {
                                    c.Item().Text(item.ItemName);
                                    if (!string.IsNullOrEmpty(item.Note))
                                    {
                                        c.Item().Text(item.Note).FontSize(8).Italic();
                                    }
                                });
                                table.Cell().PaddingVertical(3).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().PaddingVertical(3).AlignRight().Text(item.UnitPrice.ToString("N0"));
                                table.Cell().PaddingVertical(3).AlignRight().Text(item.TotalPrice.ToString("N0"));

                                if (i < itemsList.Count - 1)
                                {
                                    table.Cell().ColumnSpan(4).LineHorizontal(1).LineColor(Colors.Black);
                                }
                            }

                            table.Cell().ColumnSpan(4).BorderBottom(1).PaddingTop(2);
                        });

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                            });

                            table.Cell().PaddingVertical(2).Text("Tien hang").Italic();
                            table.Cell().PaddingVertical(2).AlignRight().Text($"{invoice.SubTotal:N0}d").Italic();

                            if (invoice.DiscountAmount > 0)
                            {
                                table.Cell().PaddingVertical(2).Text("Giam gia").Italic();
                                table.Cell().PaddingVertical(2).AlignRight().Text($"-{invoice.DiscountAmount:N0}d").Italic();
                            }

                            if (invoice.TaxAmount > 0)
                            {
                                table.Cell().PaddingVertical(2).Text("Thue (VAT)").Italic();
                                table.Cell().PaddingVertical(2).AlignRight().Text($"{invoice.TaxAmount:N0}d").Italic();
                            }

                            table.Cell().PaddingVertical(4).Text("Tong thanh toan").FontSize(12).SemiBold();
                            table.Cell().PaddingVertical(4).AlignRight().Text($"{invoice.TotalAmount:N0}d").FontSize(12).SemiBold();

                            if (invoice.AmountReceived.HasValue && invoice.AmountReceived > 0)
                            {
                                table.Cell().PaddingVertical(2).Text("Tien khach dua").Italic();
                                table.Cell().PaddingVertical(2).AlignRight().Text($"{invoice.AmountReceived:N0}d").Italic();

                                table.Cell().PaddingVertical(2).Text("Tien thua").Italic();
                                table.Cell().PaddingVertical(2).AlignRight().Text($"{invoice.AmountReturned:N0}d").Italic();
                            }
                        });
                    });

                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().PaddingTop(10).LineHorizontal(1);
                        column.Item().PaddingTop(5).Text(branding.BillFooter).FontSize(9).SemiBold();
                        column.Item().Text(branding.AppTitle).FontSize(8).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }

    }
}
