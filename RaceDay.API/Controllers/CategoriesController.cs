using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
namespace RaceDay.API.Controllers;
[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }
    // GET: api/events/{eventId}/categories
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "Event not found." });
        }
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .Select(c => new
            {
                c.CategoryId,
                c.EventId,
                c.Name,
                c.CategoryDistance,
                c.MinAge,
                c.MaxAge,
                c.MaxParticipants
            })
            .ToListAsync();
        return Ok(categories);
    }
    // POST: api/events/{eventId}/categories
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        [FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Category name is required." });
        }
        if (request.CategoryDistance <= 0)
        {
            return BadRequest(new { message = "Category distance must be greater than zero." });
        }
        if (request.MinAge.HasValue &&
            request.MaxAge.HasValue &&
            request.MinAge.Value > request.MaxAge.Value)
        {
            return BadRequest(new { message = "Minimum age cannot exceed maximum age." });
        }
        if (request.MaxParticipants.HasValue &&
            request.MaxParticipants.Value <= 0)
        {
            return BadRequest(new { message = "Maximum participants must be greater than zero." });
        }
        if (!TryGetUserId(out int organiserId))
        {
            return Unauthorized();
        }
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == eventId);
        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (eventItem.OrganiserId != organiserId)
        {
            return Forbid();
        }
        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            CategoryDistance = request.CategoryDistance,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            MaxParticipants = request.MaxParticipants
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return Created(
            $"/api/events/{eventId}/categories",
            new
            {
                category.CategoryId,
                category.EventId,
                category.Name,
                category.CategoryDistance,
                category.MinAge,
                category.MaxAge,
                category.MaxParticipants
            });
    }
    // PUT: api/categories/{id}
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Category name is required." });
        }
        if (request.CategoryDistance <= 0)
        {
            return BadRequest(new { message = "Category distance must be greater than zero." });
        }
        if (request.MinAge.HasValue &&
            request.MaxAge.HasValue &&
            request.MinAge.Value > request.MaxAge.Value)
        {
            return BadRequest(new { message = "Minimum age cannot exceed maximum age." });
        }
        if (request.MaxParticipants.HasValue &&
            request.MaxParticipants.Value <= 0)
        {
            return BadRequest(new { message = "Maximum participants must be greater than zero." });
        }
        if (!TryGetUserId(out int organiserId))
        {
            return Unauthorized();
        }
        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }
        if (category.Event == null ||
            category.Event.OrganiserId != organiserId)
        {
            return Forbid();
        }
        var currentEnrolments = await _context.Enrolments
            .CountAsync(e => e.CategoryId == id);
        if (request.MaxParticipants.HasValue &&
            request.MaxParticipants.Value < currentEnrolments)
        {
            return BadRequest(new
            {
                message = "Maximum participants cannot be less than the number of existing enrolments."
            });
        }
        category.Name = request.Name.Trim();
        category.CategoryDistance = request.CategoryDistance;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.MaxParticipants = request.MaxParticipants;
        await _context.SaveChangesAsync();
        return Ok(new
        {
            category.CategoryId,
            category.EventId,
            category.Name,
            category.CategoryDistance,
            category.MinAge,
            category.MaxAge,
            category.MaxParticipants
        });
    }
    // DELETE: api/categories/{id}
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (!TryGetUserId(out int organiserId))
        {
            return Unauthorized();
        }
        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }
        if (category.Event == null ||
            category.Event.OrganiserId != organiserId)
        {
            return Forbid();
        }
        var enrolmentIds = await _context.Enrolments
            .Where(e => e.CategoryId == id)
            .Select(e => e.EnrolmentId)
            .ToListAsync();
        var results = await _context.Results
            .Where(r => enrolmentIds.Contains(r.EnrolmentId))
            .ToListAsync();
        var enrolments = await _context.Enrolments
            .Where(e => e.CategoryId == id)
            .ToListAsync();
        _context.Results.RemoveRange(results);
        _context.Enrolments.RemoveRange(enrolments);
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}