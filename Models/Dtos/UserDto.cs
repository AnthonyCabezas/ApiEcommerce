namespace ApiEcommerce.Models.Dtos;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? Username { get; set; }
     public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

}
