using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleTestController : ControllerBase
{
    [HttpGet("organiser")]
    [Authorize(Roles = "Organiser")]
    public IActionResult OrganiserOnly()
    {
        return Ok(new
        {
            message = "Welcome, Organiser!"
        });
    }

    [HttpGet("participant")]
    [Authorize(Roles = "Participant")]
    public IActionResult ParticipantOnly()
    {
        return Ok(new
        {
            message = "Welcome, Participant!"
        });
    }

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult AuthenticatedOnly()
    {
        return Ok(new
        {
            message = "You are successfully authenticated."
        });
    }
}