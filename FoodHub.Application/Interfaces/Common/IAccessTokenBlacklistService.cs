namespace FoodHub.Application.Interfaces.Common;

public interface IAccessTokenBlacklistService
{
    Task BlacklistAsync(
        string accessToken,
        string? ipAddress = null,
        string? username = null,
        string? refreshToken = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}
