// DTOs/AuthDtos.cs
public record RegisterDto(string Username, string Password);
public record LoginDto(string Username, string Password);
public record TokenRequestDto(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);
public record LogoutDto(string RefreshToken);
