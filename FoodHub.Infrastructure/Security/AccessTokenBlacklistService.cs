using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using FoodHub.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.Security;

public class AccessTokenBlacklistService : IAccessTokenBlacklistService
{
    private const string AccessTokenBlacklistPrefix = "auth:blacklist:access:";

    private readonly ICacheService _cacheService;
    private readonly ILogger<AccessTokenBlacklistService> _logger;

    public AccessTokenBlacklistService(
        ICacheService cacheService,
        ILogger<AccessTokenBlacklistService> logger
    )
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task BlacklistAsync(
        string accessToken,
        string? ipAddress = null,
        string? username = null,
        string? refreshToken = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        JwtSecurityToken jwtToken;
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse access token for blacklist");
            return;
        }

        var jti = jwtToken.Id;
        if (string.IsNullOrWhiteSpace(jti))
        {
            _logger.LogWarning("Access token is missing jti claim; skip blacklist");
            return;
        }

        var expiresAt = jwtToken.ValidTo;
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        var entry = new AccessTokenBlacklistEntry
        {
            Jti = jti,
            Subject = jwtToken.Subject,
            Username = username ?? jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value,
            IpAddress = ipAddress,
            RefreshTokenFingerprint = ComputeFingerprint(ipAddress, username, refreshToken),
            ExpiresAt = expiresAt,
            RevokedAt = DateTime.UtcNow,
        };

        await _cacheService.SetAsync(GetCacheKey(jti), entry, ttl, cancellationToken);
    }

    public Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return Task.FromResult(false);
        }

        return _cacheService.ExistsAsync(GetCacheKey(jti), cancellationToken);
    }

    internal static string GetCacheKey(string jti)
    {
        return $"{AccessTokenBlacklistPrefix}{HashValue(jti)}";
    }

    internal static string ComputeFingerprint(
        string? ipAddress,
        string? username,
        string? refreshToken
    )
    {
        return HashValue($"{ipAddress}|{username}|{refreshToken}");
    }

    internal static string HashValue(string rawValue)
    {
        var normalized = rawValue.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private sealed class AccessTokenBlacklistEntry
    {
        public string Jti { get; init; } = string.Empty;
        public string? Subject { get; init; }
        public string? Username { get; init; }
        public string? IpAddress { get; init; }
        public string RefreshTokenFingerprint { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public DateTime RevokedAt { get; init; }
    }
}
