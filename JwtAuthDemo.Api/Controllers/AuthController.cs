
using JwtAuthDemo.Api.Data;
using JwtAuthDemo.Api.Entities.Models;
using JwtAuthDemo.Api.Services;
using JwtAuthDemo.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthController(AppDbContext db, ITokenService tokenService, IOptions<JwtSettings> options, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _jwtSettings = options.Value;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest("Username already exists");

        var user = new User
        {
            Username = dto.Username
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.Include(u => u.RefreshTokens)
                                 .SingleOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null)
            return Unauthorized();

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();

        var accessToken = _tokenService.CreateAccessToken(user);
        var refreshTokenValue = _tokenService.CreateRefreshToken();
        var refreshToken = new RefreshToken
        {
            TokenHash = _tokenService.HashToken(refreshTokenValue),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            Created = DateTime.UtcNow,
            UserId = user.Id
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshTokenValue, _jwtSettings.AccessTokenExpirationMinutes * 60));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(TokenRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.RefreshToken))
            return BadRequest("Refresh token missing");

        var tokenHash = _tokenService.HashToken(dto.RefreshToken);

        var existing = await _db.RefreshTokens
                                .Include(rt => rt.User)
                                .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (existing == null || !existing.IsActive)
            return Unauthorized("Invalid or expired refresh token");

        // Revoke current refresh token and rotate
        existing.Revoked = DateTime.UtcNow;

        var newRefreshValue = _tokenService.CreateRefreshToken();
        var newRefreshHash = _tokenService.HashToken(newRefreshValue);
        existing.ReplacedByTokenHash = newRefreshHash;

        var newRefresh = new RefreshToken
        {
            TokenHash = newRefreshHash,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            Created = DateTime.UtcNow,
            UserId = existing.UserId
        };

        _db.RefreshTokens.Add(newRefresh);
        await _db.SaveChangesAsync();

        var newAccessToken = _tokenService.CreateAccessToken(existing.User);

        return Ok(new AuthResponse(newAccessToken, newRefreshValue, _jwtSettings.AccessTokenExpirationMinutes * 60));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutDto dto)
    {
        if (string.IsNullOrEmpty(dto.RefreshToken))
            return BadRequest();

        var tokenHash = _tokenService.HashToken(dto.RefreshToken);
        var token = await _db.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if (token == null)
            return NoContent();

        token.Revoked = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        //return NoContent();
        return Ok(new { message = "You have been logged out successfully." });
    }

    // optional: logout from ALL devices
    [Authorize]
    [HttpPost("logout-all/{username}")]
    public async Task<IActionResult> LogoutAll(string username)
    {
        var user = await _db.Users.Include(u => u.RefreshTokens).SingleOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        foreach (var rt in user.RefreshTokens)
            if (rt.Revoked == null) rt.Revoked = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        //return NoContent();
        return Ok(new { message = "You have been logged out all successfully." });
    }
}
