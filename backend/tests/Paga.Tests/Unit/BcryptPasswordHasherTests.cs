using FluentAssertions;
using Paga.Infrastructure.Security;

namespace Paga.Tests.Unit;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_DeveRetornarValorDiferenteDaSenha()
    {
        // Arrange
        var password = "Senh@Forte123";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBe(password);
    }

    [Fact]
    public void Verify_DeveAceitarSenhaCorreta()
    {
        // Arrange
        var password = "Senh@Forte123";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_DeveRejeitarSenhaIncorreta()
    {
        // Arrange
        var password = "Senh@Forte123";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify("SenhaErrada456", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_DeveGerarHashesDiferentesParaMesmaSenha()
    {
        // Arrange
        var password = "Senh@Forte123";

        // Act
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }
}
