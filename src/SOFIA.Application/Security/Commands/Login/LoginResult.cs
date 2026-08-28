namespace SOFIA.Application.Security.Commands.Login;

public record LoginResult(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiry);
