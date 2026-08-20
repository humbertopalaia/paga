using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Paga.Infrastructure.Security;

namespace Paga.Tests.Unit;

public class TokenServiceTests
{
    private const string TestJwtKey = "ThisIsADevelopmentKeyMustBeAtLeast32Chars!";

    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey
            })
            .Build();

        _sut = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldContain_SubClaim_WithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = _sut.GenerateAccessToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ShouldContain_EmailClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@domain.com";

        // Act
        var token = _sut.GenerateAccessToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
    }

    [Fact]
    public void GenerateAccessToken_ShouldExpire_In30Minutes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "expiry@test.com";
        var before = DateTime.UtcNow.AddMinutes(30);

        // Act
        var token = _sut.GenerateAccessToken(userId, email);

        // Assert
        var after = DateTime.UtcNow.AddMinutes(30);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeOnOrAfter(before.AddSeconds(-5));
        jwt.ValidTo.Should().BeOnOrBefore(after.AddSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn_AtLeast43Characters()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();

        // Assert
        refreshToken.Should().HaveLength(43, "32 bytes Base64Url-encoded without padding equals 43 characters");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerate_UniqueTokens()
    {
        // Act
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => _sut.GenerateRefreshToken())
            .ToList();

        // Assert
        tokens.Distinct().Should().HaveCount(100, "all 100 refresh tokens must be unique");
    }
}
