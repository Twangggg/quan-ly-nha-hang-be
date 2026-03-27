using System.Globalization;
using FoodHub.Application.Interfaces.Branding;
using FoodHub.Domain.Entities;

namespace FoodHub.Infrastructure.Services.Branding
{
    public class BrandingFormatter : IBrandingFormatter
    {
        private readonly IBrandingSettingsProvider _brandingSettingsProvider;

        public BrandingFormatter(IBrandingSettingsProvider brandingSettingsProvider)
        {
            _brandingSettingsProvider = brandingSettingsProvider;
        }

        public string FormatDate(DateTime value) => Format(value, includeTime: false);

        public string FormatDateTime(DateTime value) => Format(value, includeTime: true);

        public string FormatCurrency(decimal value)
        {
            var settings = _brandingSettingsProvider.GetOrCreateAsync().GetAwaiter().GetResult();
            var culture = new CultureInfo(settings.Language ?? BrandingSettings.DefaultLanguage);
            var formatter = (NumberFormatInfo)culture.NumberFormat.Clone();
            formatter.CurrencySymbol = settings.Currency ?? BrandingSettings.DefaultCurrency;
            formatter.CurrencyDecimalDigits = 0;
            return value.ToString("C0", formatter);
        }

        private string Format(DateTime value, bool includeTime)
        {
            var settings = _brandingSettingsProvider.GetOrCreateAsync().GetAwaiter().GetResult();
            var timezoneId = settings.Timezone ?? BrandingSettings.DefaultTimezone;
            var cultureName = settings.Language ?? BrandingSettings.DefaultLanguage;

            var timezone = ResolveTimezone(timezoneId);
            var utcValue = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            var localValue = TimeZoneInfo.ConvertTimeFromUtc(utcValue, timezone);

            var formatString = GetFormatString(settings.DateFormat ?? BrandingSettings.DefaultDateFormat, includeTime);
            var culture = new CultureInfo(cultureName);
            return localValue.ToString(formatString, culture);
        }

        private static string GetFormatString(string dateFormat, bool includeTime)
        {
            var datePart = dateFormat switch
            {
                "MM/dd/yyyy" => "MM/dd/yyyy",
                "yyyy-MM-dd" => "yyyy-MM-dd",
                _ => "dd/MM/yyyy",
            };

            return includeTime ? $"{datePart} HH:mm" : datePart;
        }

        private static TimeZoneInfo ResolveTimezone(string timezoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById(BrandingSettings.DefaultTimezone);
            }
        }
    }
}
