using System;
using FoodHub.Application.Interfaces.Branding;
using FoodHub.Domain.Entities;
using FoodHub.Infrastructure.Services.Branding;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Branding.Services
{
    public class BrandingFormatterTests
    {
        [Fact]
        public void FormatDateTime_UsesBrandingSettings()
        {
            var settings = BrandingSettings.CreateDefault();
            settings.Update(
                "FoodHub",
                "HQ",
                "Addr",
                "0123",
                "VND",
                "dd/MM/yyyy",
                "Asia/Ho_Chi_Minh",
                "vi",
                "BILL",
                "FOOTER",
                "KDS",
                "APP",
                ""
            );

            var provider = new Mock<IBrandingSettingsProvider>();
            provider
                .Setup(p => p.GetOrCreateAsync(default))
                .ReturnsAsync(settings);

            var formatter = new BrandingFormatter(provider.Object);
            var value = new DateTime(2026, 3, 27, 13, 45, 0, DateTimeKind.Utc);
            var formatted = formatter.FormatDateTime(value);

            Assert.StartsWith("27/03/2026", formatted);
        }

        [Fact]
        public void FormatCurrency_UsesBrandingCurrency()
        {
            var settings = BrandingSettings.CreateDefault();
            settings.Update(
                "FoodHub",
                "Branch",
                "Addr",
                "0123",
                "USD",
                "dd/MM/yyyy",
                "Asia/Ho_Chi_Minh",
                "en-US",
                "BILL",
                "FOOTER",
                "KDS",
                "APP",
                ""
            );

            var provider = new Mock<IBrandingSettingsProvider>();
            provider
                .Setup(p => p.GetOrCreateAsync(default))
                .ReturnsAsync(settings);

            var formatter = new BrandingFormatter(provider.Object);
            var formatted = formatter.FormatCurrency(1500m);

            Assert.Contains("USD", formatted);
        }
    }
}
