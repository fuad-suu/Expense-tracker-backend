namespace ExpenseTracker.API.DTOs.Category;

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsOwner { get; set; } // true if this category belongs to the current user
}