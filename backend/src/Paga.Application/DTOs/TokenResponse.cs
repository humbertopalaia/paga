namespace Paga.Application.DTOs;

/// <summary>
/// Represents the token pair returned after successful authentication or refresh.
/// </summary>
public record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
