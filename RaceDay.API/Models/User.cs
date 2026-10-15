namespace RaceDay.API.Models;

public class User
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Role? Role { get; set; }
}
