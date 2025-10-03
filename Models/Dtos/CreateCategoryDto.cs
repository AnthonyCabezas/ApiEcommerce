using System.ComponentModel.DataAnnotations;

namespace ApiEcommerce.Models.Dtos;
public class CreateCategoryDto
{
    [Required(ErrorMessage = "The name is required")]
    [MaxLength(50, ErrorMessage = "The name must not exceed 50 characters")]
    [MinLength(3, ErrorMessage = "The name must be at least 3 characters long")]
    public string Name { get; set; } = string.Empty;

}