using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    // GET: api/Profile/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        if (!TryGetUserId(out int userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                Role = u.Role!.RoleName,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // PUT: api/Profile/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request)
    {
        if (!TryGetUserId(out int userId))
            return Unauthorized();

        string fullName = request.FullName.Trim();
        string email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Full name and email are required."
            });
        }

        if (fullName.Length > 100 || email.Length > 150)
        {
            return BadRequest(new
            {
                message = "Name or email is too long."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound();

        bool emailExists = await _context.Users.AnyAsync(
            u => u.Email.ToLower() == email &&
                 u.UserId != userId);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "That email is already in use."
            });
        }

        user.FullName = fullName;
        user.Email = email;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully.",
            userId = user.UserId,
            fullName = user.FullName,
            email = user.Email
        });
    }
}