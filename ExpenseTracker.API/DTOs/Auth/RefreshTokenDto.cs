using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.DTOs.Auth;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}