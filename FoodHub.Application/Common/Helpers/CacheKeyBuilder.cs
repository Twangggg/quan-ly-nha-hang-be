using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoodHub.Application.Common.Helpers;

public static class CacheKeyBuilder
{
    public static string Build(string prefix, object? value)
    {
        var payload = value is null
            ? string.Empty
            : JsonSerializer.Serialize(
                value,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return $"{prefix}:{hash}";
    }
}
