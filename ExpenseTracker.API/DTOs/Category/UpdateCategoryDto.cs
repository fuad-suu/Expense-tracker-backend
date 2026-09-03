using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Category;

public class UpdateCategoryDto
{
    [MaxLength(50)]
    public string? Name { get; set; }

    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex color e.g. #ff5733")]
    public string? Color { get; set; }

    public string? Icon { get; set; }
}