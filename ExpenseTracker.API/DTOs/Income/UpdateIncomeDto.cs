using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Income;

public class UpdateIncomeDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal? Amount { get; set; }

    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public string? Source { get; set; }
}