using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Income;

public class CreateIncomeDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    public string Source { get; set; } = "Other"; // Salary, Freelance, Business, Investment, Other
}