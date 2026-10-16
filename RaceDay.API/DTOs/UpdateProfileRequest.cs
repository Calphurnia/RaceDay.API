namespace RaceDay.API.DTOs;

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }

    public string? ContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? EmergencyContact { get; set; }
}