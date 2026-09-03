using System.Security.Claims;
using ExpenseTracker.API.DTOs.Expense;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExpensesController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/expenses
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? paymentMethod,
        [FromQuery] string? search)
    {
        var userId = GetUserId();

        var query = _db.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId)
            .AsQueryable();

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(e => e.Date >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(e => e.Date <= toUtc);
        }

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(paymentMethod))
            query = query.Where(e => e.PaymentMethod == paymentMethod);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Description.ToLower()
                .Contains(search.ToLower()));

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(expenses);
    }

    // GET api/expenses/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();

        var expense = await _db.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
            return NotFound(new { message = "Expense not found." });

        return Ok(ToDto(expense));
    }

    // POST api/expenses
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        // Validate category exists and is accessible to this user
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId &&
                (c.IsDefault || c.UserId == userId));

        if (category is null)
            return BadRequest(new { message = "Invalid category." });

        try
        {
            var expense = new Expense
            {
                Amount = dto.Amount,
                Description = dto.Description,
                Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
                PaymentMethod = dto.PaymentMethod,
                CategoryId = dto.CategoryId,
                UserId = userId
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            // Reload with category included for the response
            await _db.Entry(expense).Reference(e => e.Category).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = expense.Id }, ToDto(expense));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to save expense.", detail = ex.Message });
        }
    }

    // PUT api/expenses/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var expense = await _db.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
            return NotFound(new { message = "Expense not found." });

        // If category is being changed, validate the new one
        if (dto.CategoryId.HasValue && dto.CategoryId.Value != expense.CategoryId)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value &&
                    (c.IsDefault || c.UserId == userId));

            if (category is null)
                return BadRequest(new { message = "Invalid category." });

            expense.CategoryId = dto.CategoryId.Value;
            expense.Category = category;
        }

        if (dto.Amount.HasValue)        expense.Amount = dto.Amount.Value;
        if (dto.Description is not null) expense.Description = dto.Description;
        if (dto.PaymentMethod is not null) expense.PaymentMethod = dto.PaymentMethod;
        if (dto.Date.HasValue)
            expense.Date = DateTime.SpecifyKind(dto.Date.Value, DateTimeKind.Utc);

        expense.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToDto(expense));
    }

    // DELETE api/expenses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();

        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
            return NotFound(new { message = "Expense not found." });

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.Claims.FirstOrDefault(c =>
                        c.Type.Contains("nameidentifier"))?.Value;

        if (claim is null)
            throw new UnauthorizedAccessException("User ID claim not found in token.");

        return Guid.Parse(claim);
    }

    private static ExpenseResponseDto ToDto(Expense e) => new()
    {
        Id = e.Id,
        Amount = e.Amount,
        Description = e.Description,
        Date = e.Date,
        PaymentMethod = e.PaymentMethod,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.Name,
        CategoryColor = e.Category.Color,
        CategoryIcon = e.Category.Icon
    };
}