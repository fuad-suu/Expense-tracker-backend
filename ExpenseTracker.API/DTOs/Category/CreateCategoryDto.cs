using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Category;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex color e.g. #ff5733")]
    public string Color { get; set; } = "#6366f1";

    [Required]
    public string Icon { get; set; } = "tag";
}