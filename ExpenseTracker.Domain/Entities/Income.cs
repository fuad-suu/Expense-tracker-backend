namespace ExpenseTracker.Domain.Entities;

public class Income
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Source { get; set; } = "Other"; // Salary, Freelance, Business, Investment, Other
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}