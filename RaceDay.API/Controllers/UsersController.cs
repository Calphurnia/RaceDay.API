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
            user.ContactPhone,
            user.DateOfBirth,
            user.EmergencyContact,
            Role = user.Role != null ? user.Role.RoleName : null,
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
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return NotFound(new { message = "User not found." });
        string roleName = user.Role?.RoleName ?? string.Empty;
        if (request.FullName != null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { message = "Full name cannot be empty." });
            user.FullName = request.FullName.Trim();
        }
        if (request.Email != null)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                !request.Email.Contains('@'))
            {
                return BadRequest(new
                {
                    message = "Please provide a valid email address."
                });
            }
            string email = request.Email.Trim();
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email && u.UserId != userId);
            if (emailExists)
                return Conflict(new { message = "This email address is already in use." });
            user.Email = email;
        }
        if (roleName.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
        {
            if (request.ContactPhone != null)
            {
                if (string.IsNullOrWhiteSpace(request.ContactPhone))
                    return BadRequest(new { message = "Contact phone cannot be empty." });
                user.ContactPhone = request.ContactPhone.Trim();
            }
            if (request.DateOfBirth.HasValue ||
                request.EmergencyContact != null)
            {
                return BadRequest(new
                {
                    message = "Organisers can update their contact phone, not participant-specific fields."
                });
            }
        }
        else if (roleName.Equals("Participant", StringComparison.OrdinalIgnoreCase))
        {
            if (request.DateOfBirth.HasValue)
            {
                if (request.DateOfBirth.Value.Date > DateTime.Today)
                {
                    return BadRequest(new
                    {
                        message = "Date of birth cannot be in the future."
                    });
                }
                user.DateOfBirth = request.DateOfBirth.Value.Date;
            }
            if (request.EmergencyContact != null)
            {
                if (string.IsNullOrWhiteSpace(request.EmergencyContact))
                    return BadRequest(new { message = "Emergency contact cannot be empty." });
                user.EmergencyContact = request.EmergencyContact.Trim();
            }
            if (request.ContactPhone != null)
            {
                return BadRequest(new
                {
                    message = "Participants cannot update the organiser contact phone field."
                });
            }
        }
        else
        {
            return BadRequest(new { message = "User role is not recognised." });
        }
        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Profile updated successfully.",
            user.UserId,
            user.FullName,
            user.Email,
            user.ContactPhone,
            user.DateOfBirth,
            user.EmergencyContact,
            Role = roleName
        });
    }
    // GET: api/users/{id}
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
            Role = user.Role != null ? user.Role.RoleName : null
        });
    }
    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}