namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }

    public string Status { get; set; } = "Registered";

    public User? Participant { get; set; }
    public Event? Event { get; set; }
    public Category? Category { get; set; }
}