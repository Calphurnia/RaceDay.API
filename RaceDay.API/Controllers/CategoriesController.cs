using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET: api/events/1/categories
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventCategories(int eventId)
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
                c.MaxAge
            })
            .ToListAsync();

        return Ok(categories);
    }

    // POST: api/events/1/categories
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        [FromBody] CategoryRequest request)
    {
        if (!IsValid(request, out string error))
        {
            return BadRequest(new { message = error });
        }

        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventItem.OrganiserId != userId)
        {
            return Forbid();
        }

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            CategoryDistance = request.CategoryDistance,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
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
                category.MaxAge
            });
    }

    // PUT: api/categories/1
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] CategoryRequest request)
    {
        if (!IsValid(request, out string error))
        {
            return BadRequest(new { message = error });
        }

        if (!TryGetUserId(out int userId))
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
            category.Event.OrganiserId != userId)
        {
            return Forbid();
        }

        category.Name = request.Name.Trim();
        category.CategoryDistance = request.CategoryDistance;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            category.CategoryId,
            category.EventId,
            category.Name,
            category.CategoryDistance,
            category.MinAge,
            category.MaxAge
        });
    }

    // DELETE: api/categories/1
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (!TryGetUserId(out int userId))
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
            category.Event.OrganiserId != userId)
        {
            return Forbid();
        }

        var enrolments = await _context.Enrolments
            .Where(e => e.CategoryId == id)
            .ToListAsync();

        var enrolmentIds = enrolments
            .Select(e => e.EnrolmentId)
            .ToList();

        var results = await _context.Results
            .Where(r => enrolmentIds.Contains(r.EnrolmentId))
            .ToListAsync();

        _context.Results.RemoveRange(results);
        _context.Enrolments.RemoveRange(enrolments);
        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool IsValid(
        CategoryRequest request,
        out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "Category name is required.";
        }
        else if (request.Name.Trim().Length > 100)
        {
            error = "Category name cannot exceed 100 characters.";
        }
        else if (request.CategoryDistance <= 0)
        {
            error = "Category distance must be greater than zero.";
        }
        else if (request.MinAge.HasValue && request.MinAge.Value < 0)
        {
            error = "Minimum age cannot be negative.";
        }
        else if (request.MaxAge.HasValue && request.MaxAge.Value < 0)
        {
            error = "Maximum age cannot be negative.";
        }
        else if (request.MinAge.HasValue &&
                 request.MaxAge.HasValue &&
                 request.MinAge.Value > request.MaxAge.Value)
        {
            error = "Minimum age cannot exceed maximum age.";
        }

        return string.IsNullOrEmpty(error);
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}