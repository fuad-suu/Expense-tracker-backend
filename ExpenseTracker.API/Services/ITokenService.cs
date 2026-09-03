using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}