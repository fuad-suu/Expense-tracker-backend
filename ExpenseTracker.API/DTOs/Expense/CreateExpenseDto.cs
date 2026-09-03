using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Expense;

public class CreateExpenseDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Mobile
}