using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(
        RaceDayDbContext context,
        PasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    // POST: api/Auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new
            {
                message = "All fields are required."
            });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Password must be at least 8 characters."
            });
        }

        string email = request.Email.Trim().ToLowerInvariant();
        string roleName = request.Role.Trim();

        if (email.Length > 150 || request.FullName.Trim().Length > 100)
        {
            return BadRequest(new
            {
                message = "Name or email is too long."
            });
        }

        if (roleName.Equals("Organiser",
                StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Organiser";
        }
        else if (roleName.Equals("Participant",
                     StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Participant";
        }
        else
        {
            return BadRequest(new
            {
                message = "Role must be Organiser or Participant."
            });
        }

        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        Role? role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == roleName);

        if (role == null)
        {
            return StatusCode(500, new
            {
                message = "The required role is missing from the database."
            });
        }

        var (hash, salt) =
            _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = role.RoleId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Registration successful.",
            userId = user.UserId,
            fullName = user.FullName,
            email = user.Email,
            role = role.RoleName
        });
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new
            {
                message = "Email and password are required."
            });
        }

        string email = request.Email.Trim().ToLowerInvariant();

        User? user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null ||
            user.PasswordHash.Length != 32 ||
            user.PasswordSalt.Length != 16 ||
            !_passwordHasher.VerifyPassword(
                request.Password,
                user.PasswordHash,
                user.PasswordSalt))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        if (user.Role == null)
        {
            return Unauthorized(new
            {
                message = "User role is not configured."
            });
        }

        string token = GenerateJwtToken(user, user.Role.RoleName);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email,
            Role = user.Role.RoleName
        });
    }

    private string GenerateJwtToken(User user, string roleName)
    {
        string key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is missing.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName ?? string.Empty),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.TryParse(
                    _configuration["Jwt:ExpiryMinutes"],
                    out int minutes) && minutes > 0
                        ? minutes
                        : 60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}