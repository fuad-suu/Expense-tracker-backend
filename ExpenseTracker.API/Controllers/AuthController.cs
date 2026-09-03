using ExpenseTracker.API.DTOs.Auth;
using ExpenseTracker.API.Services;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if email already exists
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower());
        if (exists)
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var response = await BuildAuthResponse(user);
        return CreatedAtAction(nameof(Register), response);
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var response = await BuildAuthResponse(user);
        return Ok(response);
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var storedToken = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        // Revoke old refresh token (rotation)
        storedToken.IsRevoked = true;
        await _db.SaveChangesAsync();

        var response = await BuildAuthResponse(storedToken.User);
        return Ok(response);
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

        if (storedToken is not null)
        {
            storedToken.IsRevoked = true;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    // ─── Helper ───────────────────────────────────────────────────────────

    private async Task<AuthResponseDto> BuildAuthResponse(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_config["Jwt:RefreshTokenExpiryDays"]!))
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(
            double.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!));

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
            }
        };
    }
}