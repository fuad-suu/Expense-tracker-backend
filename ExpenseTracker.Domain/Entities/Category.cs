namespace ExpenseTracker.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1"; // default indigo
    public string Icon { get; set; } = "tag";      // icon name e.g. for lucide/material icons
    public bool IsDefault { get; set; } = false;   // true = seeded system category

    // null means it's a global default category, otherwise it belongs to a user
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}