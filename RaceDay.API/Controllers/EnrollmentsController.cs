using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // POST: api/categories/1/enrol
    [HttpPost("categories/{id:int}/enrol")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> EnrolInCategory(int id)
    {
        if (!TryGetUserId(out int participantId))
            return Unauthorized();

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        var alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
            e.ParticipantId == participantId &&
            e.CategoryId == id);

        if (alreadyEnrolled)
        {
            return Conflict(new
            {
                message = "You are already enrolled in this category."
            });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = category.EventId,
            CategoryId = category.CategoryId,
            EnrolmentDate = DateTime.UtcNow
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.EventId,
            enrolment.CategoryId,
            enrolment.EnrolmentDate
        });
    }

    // GET: api/users/me/enrolments
    [HttpGet("users/me/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        if (!TryGetUserId(out int participantId))
            return Unauthorized();

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.ParticipantId == participantId)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EnrolmentDate,
                e.EventId,
                EventName = e.Event != null ? e.Event.Name : null,
                e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : null,
                CategoryDistance = e.Category != null
                    ? e.Category.CategoryDistance
                    : 0
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // GET: api/categories/1/enrolments
    [HttpGet("categories/{id:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetCategoryEnrolments(int id)
    {
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event == null ||
            category.Event.OrganiserId != organiserId)
            return Forbid();

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.CategoryId == id)
            .Include(e => e.Participant)
            .OrderBy(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EnrolmentDate,
                e.ParticipantId,
                ParticipantName = e.Participant != null
                    ? e.Participant.FullName
                    : null,
                ParticipantEmail = e.Participant != null
                    ? e.Participant.Email
                    : null,
                e.EventId,
                e.CategoryId
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // DELETE: api/enrolments/1
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Withdraw(int id)
    {
        if (!TryGetUserId(out int participantId))
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != participantId)
            return Forbid();

        var hasResult = await _context.Results.AnyAsync(
            r => r.EnrolmentId == id);

        if (hasResult)
        {
            return Conflict(new
            {
                message = "An enrolment with a recorded result cannot be withdrawn."
            });
        }

        _context.Enrolments.Remove(enrolment);
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