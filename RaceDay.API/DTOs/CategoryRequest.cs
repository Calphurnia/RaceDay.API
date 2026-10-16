namespace RaceDay.API.DTOs;

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal CategoryDistance { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}