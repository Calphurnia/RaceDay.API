using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET: api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        if (!TryGetUserId(out int userId))
            return Unauthorized();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            Role = user.Role?.RoleName,
            user.CreatedAt
        });
    }

    // PUT: api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request)
    {
        if (!TryGetUserId(out int userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                message = "Full name and email are required."
            });
        }

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (fullName.Length > 100 || email.Length > 150 ||
            !email.Contains('@'))
        {
            return BadRequest(new
            {
                message = "Please provide a valid name and email."
            });
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var emailInUse = await _context.Users.AnyAsync(u =>
            u.UserId != userId &&
            u.Email.ToLower() == email);

        if (emailInUse)
        {
            return Conflict(new
            {
                message = "That email address is already registered."
            });
        }

        user.FullName = fullName;
        user.Email = email;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            Role = user.Role?.RoleName
        });
    }

    // GET: api/users/10
    // Organisers can view a user's public profile.
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.UserId,
            user.FullName,
            Role = user.Role?.RoleName
        });
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}