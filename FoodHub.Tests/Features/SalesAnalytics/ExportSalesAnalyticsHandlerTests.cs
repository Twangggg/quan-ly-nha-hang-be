using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SalesAnalytics.Queries.Export;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SalesAnalytics
{
    public class ExportSalesAnalyticsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ISalesExcelService> _mockExcelService;
        private readonly Mock<ILogger<ExportSalesAnalyticsHandler>> _mockLogger;
        private readonly ExportSalesAnalyticsHandler _handler;

        public ExportSalesAnalyticsHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMediator = new Mock<IMediator>();
            _mockExcelService = new Mock<ISalesExcelService>();
            _mockLogger = new Mock<ILogger<ExportSalesAnalyticsHandler>>();
            _handler = new ExportSalesAnalyticsHandler(
                _mockUow.Object,
                _mockMediator.Object,
                _mockExcelService.Object,
                _mockLogger.Object
            );
        }

        private void SetupOrderRepo(IEnumerable<FoodHub.Domain.Entities.Order> orders)
        {
            var mockRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            mockRepo.Setup(r => r.Query()).Returns(orders.AsQueryable().BuildMock());
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Order>())
                .Returns(mockRepo.Object);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldReturnExcelBytes()
        {
            // Arrange
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());

            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetBestSellersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result<GetBestSellersResponse>.Success(
                        new GetBestSellersResponse { Items = new List<BestSellerDto>() }
                    )
                );

            _mockMediator
                .Setup(m =>
                    m.Send(It.IsAny<GetCategoryReportQuery>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<GetCategoryReportResponse>.Success(
                        new GetCategoryReportResponse { Items = new List<CategoryReportDto>() }
                    )
                );

            byte[] expectedBytes = { 1, 2, 3 };
            _mockExcelService
                .Setup(s =>
                    s.ExportAnalyticsToExcel(
                        It.IsAny<string>(),
                        It.IsAny<FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport.GetDailyReportResponse>(),
                        It.IsAny<List<BestSellerDto>>(),
                        It.IsAny<List<CategoryReportDto>>()
                    )
                )
                .Returns(expectedBytes);

            var query = new ExportSalesAnalyticsQuery { Year = 2026, Month = 3 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public async Task Handle_WithNoOrdersInRange_ShouldStillReturnExcel()
        {
            // Arrange
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetBestSellersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetBestSellersResponse>.Success(new GetBestSellersResponse()));
            _mockMediator
                .Setup(m =>
                    m.Send(It.IsAny<GetCategoryReportQuery>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<GetCategoryReportResponse>.Success(new GetCategoryReportResponse())
                );

            byte[] expectedBytes = { 0, 0, 0 };
            _mockExcelService
                .Setup(s =>
                    s.ExportAnalyticsToExcel(
                        It.IsAny<string>(),
                        It.IsAny<FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport.GetDailyReportResponse>(),
                        It.IsAny<List<BestSellerDto>>(),
                        It.IsAny<List<CategoryReportDto>>()
                    )
                )
                .Returns(expectedBytes);

            var query = new ExportSalesAnalyticsQuery { Date = new DateOnly(2026, 3, 10) };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WhenExcelServiceThrows_ShouldPropagateException()
        {
            // Arrange
            SetupOrderRepo(new List<FoodHub.Domain.Entities.Order>());
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetBestSellersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetBestSellersResponse>.Success(new GetBestSellersResponse()));
            _mockMediator
                .Setup(m =>
                    m.Send(It.IsAny<GetCategoryReportQuery>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<GetCategoryReportResponse>.Success(new GetCategoryReportResponse())
                );

            _mockExcelService
                .Setup(s =>
                    s.ExportAnalyticsToExcel(
                        It.IsAny<string>(),
                        It.IsAny<FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport.GetDailyReportResponse>(),
                        It.IsAny<List<BestSellerDto>>(),
                        It.IsAny<List<CategoryReportDto>>()
                    )
                )
                .Throws(new Exception("Excel Service Error"));

            var query = new ExportSalesAnalyticsQuery();

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Excel Service Error");
        }
    }
}
