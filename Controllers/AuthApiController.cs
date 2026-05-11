using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OfisYonetimSistemi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly LoginAttemptTracker _loginAttemptTracker;

    public AuthApiController(
        AppDbContext context,
        IConfiguration configuration,
        LoginAttemptTracker loginAttemptTracker)
    {
        _context = context;
        _configuration = configuration;
        _loginAttemptTracker = loginAttemptTracker;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var email = NormalizeEmail(request.Email);
        var ipAddress = ClientIpAddress();
        var lockoutStatus = _loginAttemptTracker.GetLockoutStatus(email, ipAddress);

        if (lockoutStatus.IsLocked)
        {
            return TooManyLoginAttempts(lockoutStatus);
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == request.Password);

        if (user == null)
        {
            var failedStatus = _loginAttemptTracker.RecordFailedAttempt(email, ipAddress);

            if (failedStatus.IsLocked)
            {
                return TooManyLoginAttempts(failedStatus);
            }

            return Unauthorized(new { message = "Email veya sifre hatali." });
        }

        _loginAttemptTracker.ResetEmail(email);

        var expiresAt = DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes());
        var token = CreateToken(user, expiresAt);

        return Ok(new LoginResponse(
            token,
            expiresAt,
            new LoginUserResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.Role?.Name ?? string.Empty,
                user.CompanyName,
                user.CompanySize)));
    }

    private string CreateToken(User user, DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName)
        };

        if (!string.IsNullOrWhiteSpace(user.Role?.Name))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenLifetimeMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 60;
    }

    private IActionResult TooManyLoginAttempts(LoginLockoutStatus lockoutStatus)
    {
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(lockoutStatus.RetryAfter.TotalSeconds));
        Response.Headers.RetryAfter = retryAfterSeconds.ToString();

        return StatusCode(StatusCodes.Status429TooManyRequests, new
        {
            message = "Cok fazla hatali giris denemesi yapildi. Lutfen biraz sonra tekrar deneyin.",
            retryAfterSeconds
        });
    }

    private string ClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}

public class LoginRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    LoginUserResponse User);

public record LoginUserResponse(
    int Id,
    string FullName,
    string Email,
    string Role,
    string CompanyName,
    int CompanySize);
