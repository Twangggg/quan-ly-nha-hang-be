using FluentAssertions;
using FoodHub.Application.Constants;
using FoodHub.Application.Services;

namespace FoodHub.Tests.Services
{
    public class MessageServiceTests
    {
        private readonly MessageService _messageService = new();

        [Theory]
        [InlineData(MessageKeys.Profile.EmployeeIdRequired)]
        [InlineData(MessageKeys.Profile.FullNameRequired)]
        [InlineData(MessageKeys.Profile.FullNameMaxLength)]
        [InlineData(MessageKeys.Profile.EmailRequired)]
        [InlineData(MessageKeys.Profile.EmailInvalid)]
        [InlineData(MessageKeys.Profile.PhoneRequired)]
        [InlineData(MessageKeys.Profile.PhoneInvalid)]
        [InlineData(MessageKeys.Profile.DateOfBirthMustBePast)]
        public void HasKey_Should_Return_True_For_Profile_Validation_Keys(string key)
        {
            var hasKey = _messageService.HasKey(key);

            hasKey.Should().BeTrue();
        }

        [Fact]
        public void GetMessage_Should_Return_Localized_Message_For_ProfilePhoneInvalid()
        {
            var message = _messageService.GetMessage(MessageKeys.Profile.PhoneInvalid);

            message.Should().NotBe(MessageKeys.Profile.PhoneInvalid);
            message.Should().NotBeNullOrWhiteSpace();
        }
    }
}
