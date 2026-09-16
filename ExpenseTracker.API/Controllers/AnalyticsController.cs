using System.Security.Claims;
using ExpenseTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AnalyticsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/analytics/monthly-trend
    // Returns last 6 months of income, expenses and savings
    [HttpGet("monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend()
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var months = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-i))
            .OrderBy(d => d)
            .ToList();

        var expenses = await _db.Expenses
            .Where(e => e.UserId == userId &&
                e.Date >= months.First().AddDays(-months.First().Day + 1))
            .ToListAsync();

        var incomes = await _db.Incomes
            .Where(i => i.UserId == userId &&
                i.Date >= months.First().AddDays(-months.First().Day + 1))
            .ToListAsync();

        var result = months.Select(m => {
            var monthIncome = incomes
                .Where(i => i.Date.Year == m.Year && i.Date.Month == m.Month)
                .Sum(i => i.Amount);

            var monthExpense = expenses
                .Where(e => e.Date.Year == m.Year && e.Date.Month == m.Month)
                .Sum(e => e.Amount);

            return new
            {
                month = m.ToString("MMM yyyy"),
                income = monthIncome,
                expenses = monthExpense,
                savings = monthIncome - monthExpense
            };
        });

        return Ok(result);
    }

    // GET api/analytics/category-breakdown
    // Returns expense breakdown by category
    [HttpGet("category-breakdown")]
    public async Task<IActionResult> GetCategoryBreakdown()
    {
        var userId = GetUserId();

        var breakdown = await _db.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId)
            .GroupBy(e => new { e.CategoryId, e.Category.Name, e.Category.Color })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                categoryName = g.Key.Name,
                categoryColor = g.Key.Color,
                total = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.total)
            .ToListAsync();

        return Ok(breakdown);
    }

    // GET api/analytics/income-by-source
    // Returns income breakdown by source
    [HttpGet("income-by-source")]
    public async Task<IActionResult> GetIncomeBySource()
    {
        var userId = GetUserId();

        var breakdown = await _db.Incomes
            .Where(i => i.UserId == userId)
            .GroupBy(i => i.Source)
            .Select(g => new
            {
                source = g.Key,
                total = g.Sum(i => i.Amount)
            })
            .OrderByDescending(x => x.total)
            .ToListAsync();

        return Ok(breakdown);
    }

    // GET api/analytics/savings-summary
    // Returns savings summary for current month
    [HttpGet("savings-summary")]
    public async Task<IActionResult> GetSavingsSummary()
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var monthIncome = await _db.Incomes
            .Where(i => i.UserId == userId &&
                i.Date >= monthStart && i.Date < monthEnd)
            .SumAsync(i => i.Amount);

        var monthExpenses = await _db.Expenses
            .Where(e => e.UserId == userId &&
                e.Date >= monthStart && e.Date < monthEnd)
            .SumAsync(e => e.Amount);

        var savings = monthIncome - monthExpenses;
        var savingsRate = monthIncome > 0
            ? Math.Round((savings / monthIncome) * 100, 1)
            : 0;

        return Ok(new
        {
            income   = monthIncome,
            expenses = monthExpenses,
            savings,
            savingsRate,
            month = now.ToString("MMM yyyy")
        });
    }

    // ── Helper ────────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.Claims.FirstOrDefault(c =>
                        c.Type.Contains("nameidentifier"))?.Value;

        if (claim is null)
            throw new UnauthorizedAccessException("User ID claim not found.");

        return Guid.Parse(claim);
    }
}