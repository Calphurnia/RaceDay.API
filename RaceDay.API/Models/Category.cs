namespace RaceDay.API.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal CategoryDistance { get; set; }

    public Event? Event { get; set; }
}