using System;

namespace FoodHub.Application.Interfaces.Branding
{
    public interface IBrandingFormatter
    {
        string FormatDate(DateTime value);
        string FormatDateTime(DateTime value);
        string FormatCurrency(decimal value);
    }
}
