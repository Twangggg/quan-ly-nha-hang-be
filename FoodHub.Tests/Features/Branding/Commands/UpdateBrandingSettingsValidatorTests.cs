using FluentAssertions;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Branding.Settings.Commands.UpdateBrandingSettings;
using FoodHub.Application.Interfaces.Common;
using Moq;

namespace FoodHub.Tests.Features.Branding.Commands
{
    public class UpdateBrandingSettingsValidatorTests
    {
        private readonly Mock<IMessageService> _messageService = new();

        public UpdateBrandingSettingsValidatorTests()
        {
            _messageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns<string>(key => key);
        }

        [Fact]
        public async Task ValidateAsync_Should_Fail_When_Phone_Is_Invalid()
        {
            var validator = new UpdateBrandingSettingsValidator(_messageService.Object);

            var result = await validator.ValidateAsync(CreateCommand(phone: "12345"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(x =>
                x.ErrorMessage == MessageKeys.Profile.PhoneInvalid
            );
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_When_Phone_Is_Empty()
        {
            var validator = new UpdateBrandingSettingsValidator(_messageService.Object);

            var result = await validator.ValidateAsync(CreateCommand(phone: string.Empty));

            result.IsValid.Should().BeTrue();
        }

        private static UpdateBrandingSettingsCommand CreateCommand(string phone)
        {
            return new UpdateBrandingSettingsCommand(
                "FoodHub Restaurant",
                "District 1",
                "123 Street",
                phone,
                "VND",
                "dd/MM/yyyy",
                "Asia/Ho_Chi_Minh",
                "vi",
                "Hoa don",
                "Cam on",
                "KDS",
                "FoodHub",
                string.Empty,
                "Thứ 2 - Chủ Nhật",
                "08:00 - 22:00",
                string.Empty,
                string.Empty
            );
        }
    }
}
