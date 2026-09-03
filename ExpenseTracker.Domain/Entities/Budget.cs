namespace ExpenseTracker.Domain.Entities;

public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal LimitAmount { get; set; }

    // e.g. "2026-09" — year-month this budget applies to
    public string MonthYear { get; set; } = string.Empty;

    // null = overall monthly budget, otherwise per-category
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}