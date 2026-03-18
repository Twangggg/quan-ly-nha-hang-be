using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf;
using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Billing.Queries
{
    public class ExportPreCheckBillPdfHandlerTests
    {
        private readonly Mock<IMediator> _mockMediator = new();
        private readonly Mock<IPdfService> _mockPdfService = new();
        private readonly Mock<ILogger<ExportPreCheckBillPdfHandler>> _mockLogger = new();

        private ExportPreCheckBillPdfHandler CreateHandler() =>
            new(_mockMediator.Object, _mockPdfService.Object, _mockLogger.Object);

        [Fact]
        public async Task Handle_Should_ReturnPdfResponse_When_PreCheckBillExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var query = new ExportPreCheckBillPdfQuery { OrderId = orderId };
            var preCheckBill = new GetPreCheckBillResponse
            {
                OrderId = orderId,
                OrderCode = "ORD-20260315-0001",
                TableNumber = 5,
                EmployeeName = "Nguyen Van A",
                PrintedAt = DateTime.UtcNow,
                Items = new List<PreCheckBillItemDto>
                {
                    new()
                    {
                        ItemName = "Pho Bo",
                        Quantity = 2,
                        UnitPrice = 50000,
                        LineTotal = 100000,
                    },
                },
                SubTotal = 100000,
                Discount = 0,
                Vat = 0,
                TotalAmount = 100000,
            };
            var pdfBytes = new byte[] { 1, 2, 3, 4 };

            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetPreCheckBillQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetPreCheckBillResponse>.Success(preCheckBill));
            _mockPdfService.Setup(x => x.GeneratePreCheckBill(preCheckBill)).Returns(pdfBytes);

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Content.Should().Equal(pdfBytes);
            result.Data.FileName.Should().MatchRegex(
                @"^TamTinh_Ban05_ORD-20260315-0001_\d{8}_\d{4}\.pdf$"
            );
        }

        [Fact]
        public async Task Handle_Should_PropagateFailure_When_PreCheckBillQueryFails()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var query = new ExportPreCheckBillPdfQuery { OrderId = orderId };

            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetPreCheckBillQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetPreCheckBillResponse>.Failure("Order not found", ResultErrorType.NotFound));

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Order not found");
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            _mockPdfService.Verify(x => x.GeneratePreCheckBill(It.IsAny<GetPreCheckBillResponse>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Throw_When_PdfGenerationFails()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var query = new ExportPreCheckBillPdfQuery { OrderId = orderId };
            var preCheckBill = new GetPreCheckBillResponse
            {
                OrderId = orderId,
                OrderCode = "ORD-20260315-0001",
                EmployeeName = "Nguyen Van A",
                PrintedAt = DateTime.UtcNow,
                Items = new List<PreCheckBillItemDto>(),
                TotalAmount = 100000,
            };

            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetPreCheckBillQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetPreCheckBillResponse>.Success(preCheckBill));
            _mockPdfService
                .Setup(x => x.GeneratePreCheckBill(preCheckBill))
                .Throws(new InvalidOperationException("PDF generation failed"));

            var handler = CreateHandler();

            // Act
            var action = async () => await handler.Handle(query, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("PDF generation failed");
        }
    }
}
