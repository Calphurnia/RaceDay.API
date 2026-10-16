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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // POST: api/enrolments/1/result
    [HttpPost("enrolments/{id:int}/result")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> RecordResult(
        int id,
        [FromBody] ResultRequest request)
    {
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();

        if (request.FinishTime < TimeSpan.Zero ||
            request.FinishTime == TimeSpan.MaxValue ||
            request.Position <= 0)
        {
            return BadRequest(new
            {
                message = "Provide a valid finishing time and a position greater than zero."
            });
        }

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event == null ||
            enrolment.Event.OrganiserId != organiserId)
            return Forbid();

        var resultExists = await _context.Results
            .AnyAsync(r => r.EnrolmentId == id);

        if (resultExists)
        {
            return Conflict(new
            {
                message = "A result has already been recorded for this enrolment."
            });
        }

        // Do not allow the same position to be assigned twice
        // within the same category.
        var positionExists = await _context.Results
            .AnyAsync(r =>
                r.FinishPosition == request.Position &&
                r.Enrolment != null &&
                r.Enrolment.CategoryId == enrolment.CategoryId);

        if (positionExists)
        {
            return Conflict(new
            {
                message = "That finishing position is already assigned in this category."
            });
        }

        var result = new Result
        {
            EnrolmentId = id,
            FinishTime = request.FinishTime,
            FinishPosition = request.Position,
            RecordedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.FinishPosition,
            result.RecordedAt
        });
    }

    // GET: api/categories/1/results
    [HttpGet("categories/{id:int}/results")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryResults(int id)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == id);

        if (!categoryExists)
            return NotFound(new { message = "Category not found." });

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment != null &&
                        r.Enrolment.CategoryId == id)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Participant)
            .OrderBy(r => r.FinishPosition ?? int.MaxValue)
            .ThenBy(r => r.FinishTime)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                ParticipantId = r.Enrolment!.ParticipantId,
                ParticipantName = r.Enrolment.Participant != null
                    ? r.Enrolment.Participant.FullName
                    : null,
                r.FinishTime,
                r.FinishPosition,
                r.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    // GET: api/enrolments/1/result
    [HttpGet("enrolments/{id:int}/result")]
    [Authorize]
    public async Task<IActionResult> GetEnrolmentResult(int id)
    {
        if (!TryGetUserId(out int userId))
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        bool isParticipantOwner = enrolment.ParticipantId == userId;

        bool isOwningOrganiser =
            User.IsInRole("Organiser") &&
            enrolment.Event != null &&
            enrolment.Event.OrganiserId == userId;

        if (!isParticipantOwner && !isOwningOrganiser)
            return Forbid();

        var result = await _context.Results
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EnrolmentId == id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "No result has been recorded for this enrolment yet."
            });
        }

        return Ok(new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.FinishPosition,
            result.RecordedAt
        });
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}