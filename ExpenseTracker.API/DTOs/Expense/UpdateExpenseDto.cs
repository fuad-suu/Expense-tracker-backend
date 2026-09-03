using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Expense;

public class UpdateExpenseDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal? Amount { get; set; }

    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public Guid? CategoryId { get; set; }
    public string? PaymentMethod { get; set; }
}