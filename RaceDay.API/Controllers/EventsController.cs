using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
namespace RaceDay.API.Controllers;
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }
    // GET: api/events
    // Optional filters: ?date=2026-10-20&location=Johannesburg
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTime? date,
        [FromQuery] string? location)
    {
        var query = _context.Events
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .AsQueryable();
        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            var nextDate = selectedDate.AddDays(1);
            query = query.Where(e =>
                e.EventDate >= selectedDate &&
                e.EventDate < nextDate);
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(e =>
                e.Location.Contains(location));
        }
        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventResponse
            {
                EventId = e.EventId,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Distance = e.Distance,
                EventType = e.EventType,
                OrganiserId = e.OrganiserId
            })
            .ToListAsync();
        return Ok(events);
    }
    // GET: api/events/{id}
    [HttpGet("{id:int}", Name = "GetEventById")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventById(int id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e =>
                e.EventId == id && e.IsPublished);
        if (eventItem == null)
            return NotFound(new { message = "Published event not found." });
        return Ok(new
        {
            eventItem.EventId,
            eventItem.Name,
            eventItem.Description,
            eventItem.EventDate,
            eventItem.Location,
            eventItem.Distance,
            eventItem.EventType,
            eventItem.OrganiserId,
            eventItem.IsPublished,
            Categories = eventItem.Categories.Select(c => new
            {
                c.CategoryId,
                c.Name,
                c.CategoryDistance,
                c.MinAge,
                c.MaxAge,
                c.MaxParticipants
            })
        });
    }
    // POST: api/events
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(
        [FromBody] EventRequest request)
    {
        if (!IsValidRequest(request, out string error))
            return BadRequest(new { message = error });
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();
        var eventItem = new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = request.EventType.Trim(),
            OrganiserId = organiserId,
            IsPublished = true
        };
        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync();
        return CreatedAtRoute(
            "GetEventById",
            new { id = eventItem.EventId },
            ToResponse(eventItem));
    }
    // PUT: api/events/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] EventRequest request)
    {
        if (!IsValidRequest(request, out string error))
            return BadRequest(new { message = error });
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });
        if (eventItem.OrganiserId != organiserId)
            return Forbid();
        eventItem.Name = request.Name.Trim();
        eventItem.Description = request.Description?.Trim();
        eventItem.EventDate = request.EventDate;
        eventItem.Location = request.Location.Trim();
        eventItem.Distance = request.Distance;
        eventItem.EventType = request.EventType.Trim();
        await _context.SaveChangesAsync();
        return Ok(ToResponse(eventItem));
    }
    // DELETE: api/events/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });
        if (eventItem.OrganiserId != organiserId)
            return Forbid();
        var enrolmentIds = await _context.Enrolments
            .Where(e => e.EventId == id)
            .Select(e => e.EnrolmentId)
            .ToListAsync();
        var results = await _context.Results
            .Where(r => enrolmentIds.Contains(r.EnrolmentId))
            .ToListAsync();
        var enrolments = await _context.Enrolments
            .Where(e => e.EventId == id)
            .ToListAsync();
        var categories = await _context.Categories
            .Where(c => c.EventId == id)
            .ToListAsync();
        _context.Results.RemoveRange(results);
        _context.Enrolments.RemoveRange(enrolments);
        _context.Categories.RemoveRange(categories);
        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    private bool IsValidRequest(
        EventRequest request,
        out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "Event name is required.";
        }
        else if (request.Name.Trim().Length > 150)
        {
            error = "Event name cannot exceed 150 characters.";
        }
        else if (string.IsNullOrWhiteSpace(request.Location))
        {
            error = "Event location is required.";
        }
        else if (request.Distance <= 0)
        {
            error = "Distance must be greater than zero.";
        }
        else if (string.IsNullOrWhiteSpace(request.EventType) ||
                 !new[] { "running", "walking", "cycling" }
                     .Contains(
                         request.EventType.Trim(),
                         StringComparer.OrdinalIgnoreCase))
        {
            error = "Event type must be running, walking or cycling.";
        }
        else if (request.EventDate == default)
        {
            error = "A valid event date is required.";
        }
        return string.IsNullOrEmpty(error);
    }
    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
    private static EventResponse ToResponse(Event eventItem)
    {
        return new EventResponse
        {
            EventId = eventItem.EventId,
            Name = eventItem.Name,
            Description = eventItem.Description,
            EventDate = eventItem.EventDate,
            Location = eventItem.Location,
            Distance = eventItem.Distance,
            EventType = eventItem.EventType,
            OrganiserId = eventItem.OrganiserId
        };
    }
}