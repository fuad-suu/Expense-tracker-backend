using System.Security.Claims;
using ExpenseTracker.API.DTOs.Income;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncomeController : ControllerBase
{
    private readonly AppDbContext _db;

    public IncomeController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/income
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? source)
    {
        var userId = GetUserId();

        var query = _db.Incomes
            .Where(i => i.UserId == userId)
            .AsQueryable();

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(i => i.Date >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(i => i.Date <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(i => i.Source == source);

        var incomes = await query
            .OrderByDescending(i => i.Date)
            .Select(i => ToDto(i))
            .ToListAsync();

        return Ok(incomes);
    }

    // GET api/income/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();

        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (income is null)
            return NotFound(new { message = "Income record not found." });

        return Ok(ToDto(income));
    }

    // POST api/income
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncomeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        try
        {
            var income = new Income
            {
                Amount = dto.Amount,
                Description = dto.Description,
                Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
                Source = dto.Source,
                UserId = userId
            };

            _db.Incomes.Add(income);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = income.Id }, ToDto(income));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to save income.", detail = ex.Message });
        }
    }

    // PUT api/income/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncomeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (income is null)
            return NotFound(new { message = "Income record not found." });

        if (dto.Amount.HasValue)   income.Amount = dto.Amount.Value;
        if (dto.Description != null) income.Description = dto.Description;
        if (dto.Date.HasValue) income.Date = DateTime.SpecifyKind(dto.Date.Value, DateTimeKind.Utc);
        if (dto.Source != null)    income.Source = dto.Source;

        income.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToDto(income));
    }

    // DELETE api/income/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();

        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (income is null)
            return NotFound(new { message = "Income record not found." });

        _db.Incomes.Remove(income);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        // Try both the short name and the full URI form
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.Claims.FirstOrDefault(c =>
                        c.Type.Contains("nameidentifier"))?.Value;

        if (claim is null)
            throw new UnauthorizedAccessException("User ID claim not found in token.");

        return Guid.Parse(claim);
    }

    private static IncomeResponseDto ToDto(Income i) => new()
    {
        Id = i.Id,
        Amount = i.Amount,
        Description = i.Description,
        Date = i.Date,
        Source = i.Source,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt
    };
}