using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodHub.Tests.Infrastructure.Security;

public class AccessTokenBlacklistServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly AccessTokenBlacklistService _service;

    public AccessTokenBlacklistServiceTests()
    {
        _service = new AccessTokenBlacklistService(
            _cacheServiceMock.Object,
            Mock.Of<ILogger<AccessTokenBlacklistService>>()
        );
    }

    [Fact]
    public async Task BlacklistAsync_Should_StoreBlacklistedTokenByJti()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        var token = CreateJwt("token-jti", "user-1", "manager.emp001", expiresAt);
        var expectedKey = BuildCacheKey("token-jti");

        await _service.BlacklistAsync(token, "127.0.0.1", "manager.emp001", "refresh-token");

        _cacheServiceMock.Invocations.Should().ContainSingle();
        var invocation = _cacheServiceMock.Invocations.Single();
        invocation.Method.Name.Should().Be(nameof(ICacheService.SetAsync));
        invocation.Arguments[0].Should().Be(expectedKey);
        invocation.Arguments[2].Should().BeOfType<TimeSpan>();

        var ttl = (TimeSpan)invocation.Arguments[2];
        ttl.Should().BeGreaterThan(TimeSpan.FromMinutes(9));
        ttl.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task IsBlacklistedAsync_Should_CheckHashedJtiKey()
    {
        var expectedKey = BuildCacheKey("token-jti");
        _cacheServiceMock
            .Setup(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.IsBlacklistedAsync("token-jti");

        result.Should().BeTrue();
    }

    private static string CreateJwt(
        string jti,
        string sub,
        string username,
        DateTime expiresAt
    )
    {
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, sub),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
            ],
            expires: expiresAt
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string BuildCacheKey(string jti)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jti)));
        return $"auth:blacklist:access:{hash}";
    }
}
