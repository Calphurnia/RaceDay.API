using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
namespace RaceDay.API.Controllers;
/// <summary>
/// Manages race events, including viewing, creating,
/// updating and deleting events.
/// </summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    /// <summary>
    /// Initialises the EventsController.
    /// </summary>
    /// <param name="context">The RaceDay database context.</param>
    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Retrieves all published events.
    /// </summary>
    /// <param name="date">Optional event date filter.</param>
    /// <param name="location">Optional event location filter.</param>
    /// <returns>A list of published events matching the filters.</returns>
    /// <response code="200">Returns the list of events.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    /// <summary>
    /// Retrieves a published event and its categories.
    /// </summary>
    /// <param name="id">The ID of the event.</param>
    /// <returns>The event details and associated categories.</returns>
    /// <response code="200">Returns the event.</response>
    /// <response code="404">The event was not found or is unpublished.</response>
    [HttpGet("{id:int}", Name = "GetEventById")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(int id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e =>
                e.EventId == id && e.IsPublished);
        if (eventItem == null)
        {
            return NotFound(new
            {
                message = "Published event not found."
            });
        }
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
    /// <summary>
    /// Creates a new published event for the authenticated organiser.
    /// </summary>
    /// <param name="request">The event information.</param>
    /// <returns>The newly created event.</returns>
    /// <response code="201">The event was created.</response>
    /// <response code="400">The event information is invalid.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not an organiser.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] EventRequest request)
    {
        if (!IsValidRequest(request, out string error))
        {
            return BadRequest(new { message = error });
        }
        if (!TryGetUserId(out int organiserId))
        {
            return Unauthorized();
        }
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
    /// <summary>
    /// Updates an event owned by the authenticated organiser.
    /// </summary>
    /// <param name="id">The ID of the event to update.</param>
    /// <param name="request">The updated event information.</param>
    /// <returns>The updated event.</returns>
    /// <response code="200">The event was updated.</response>
    /// <response code="400">The supplied information is invalid.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">The event was not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] EventRequest request)
    {
        if (!IsValidRequest(request, out string error))
        {
            return BadRequest(new { message = error });
        }
        if (!TryGetUserId(out int organiserId))
        {
            return Unauthorized();
        }
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (eventItem.OrganiserId != organiserId)
        {
            return Forbid();
        }
        eventItem.Name = request.Name.Trim();
        eventItem.Description = request.Description?.Trim();
        eventItem.EventDate = request.EventDate;
        eventItem.Location = request.Location.Trim();
        eventItem.Distance = request.Distance;
        eventItem.EventType = request.EventType.Trim();
        await _context.SaveChangesAsync();
        return Ok(ToResponse(eventItem));
    }
    /// <summary>
    /// Deletes an event owned by the authenticated organiser,
    /// including its related results, enrolments and categories.
    /// </summary>
    /// <param name="id">The ID of the event to delete.</param>
    /// <returns>No content when deletion succeeds.</returns>
    /// <response code="204">The event was deleted.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">The event was not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!TryGetUserId(out int organiserId))
        {
            return Unauthorized();
        }
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }
        if (eventItem.OrganiserId != organiserId)
        {
            return Forbid();
        }
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
    /// <summary>
    /// Validates the event details supplied by the client.
    /// </summary>
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
    /// <summary>
    /// Gets the authenticated user's ID from the JWT claims.
    /// </summary>
    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
    /// <summary>
    /// Converts an event model into an API response.
    /// </summary>
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