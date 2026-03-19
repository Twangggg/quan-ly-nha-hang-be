namespace FoodHub.Application.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public static DateTime? ToUtc(this DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.ToUtc();
    }
}
