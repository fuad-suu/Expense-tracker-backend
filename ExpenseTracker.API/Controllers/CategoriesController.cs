using System.Security.Claims;
using ExpenseTracker.API.DTOs.Category;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/categories
    // Returns default categories + the current user's custom categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();

        var categories = await _db.Categories
            .Where(c => c.IsDefault == true || c.UserId == userId)
            .OrderBy(c => c.IsDefault ? 0 : 1) // defaults first
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                Icon = c.Icon,
                IsDefault = c.IsDefault,
                IsOwner = c.UserId == userId
            })
            .ToListAsync();

        return Ok(categories);
    }

    // GET api/categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id &&
                (c.IsDefault == true || c.UserId == userId));

        if (category is null)
            return NotFound(new { message = "Category not found." });

        return Ok(ToDto(category, userId));
    }

    // POST api/categories
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        // Prevent duplicate category names for this user
        var duplicate = await _db.Categories.AnyAsync(c =>
            c.Name.ToLower() == dto.Name.ToLower() &&
            (c.IsDefault || c.UserId == userId));

        if (duplicate)
            return Conflict(new { message = "A category with that name already exists." });

        var category = new Category
        {
            Name = dto.Name,
            Color = dto.Color,
            Icon = dto.Icon,
            IsDefault = false,
            UserId = userId
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToDto(category, userId));
    }

    // PUT api/categories/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        // Not found OR it's a default category (user can't edit defaults)
        if (category is null)
            return NotFound(new { message = "Category not found or you do not have permission to edit it." });

        if (category.IsDefault)
            return Forbid();

        // Check for duplicate name if name is being changed
        if (dto.Name is not null && dto.Name.ToLower() != category.Name.ToLower())
        {
            var duplicate = await _db.Categories.AnyAsync(c =>
                c.Name.ToLower() == dto.Name.ToLower() &&
                (c.IsDefault || c.UserId == userId) &&
                c.Id != id);

            if (duplicate)
                return Conflict(new { message = "A category with that name already exists." });
        }

        if (dto.Name is not null)  category.Name = dto.Name;
        if (dto.Color is not null) category.Color = dto.Color;
        if (dto.Icon is not null)  category.Icon = dto.Icon;

        await _db.SaveChangesAsync();

        return Ok(ToDto(category, userId));
    }

    // DELETE api/categories/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();

        var category = await _db.Categories
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category is null)
            return NotFound(new { message = "Category not found or you do not have permission to delete it." });

        if (category.IsDefault)
            return BadRequest(new { message = "Default categories cannot be deleted." });

        // Can't delete if expenses are using this category
        if (category.Expenses.Any())
            return BadRequest(new { message = $"Cannot delete category — it has {category.Expenses.Count} expense(s) attached. Reassign them first." });

        _db.Categories.Remove(category);
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

    private static CategoryResponseDto ToDto(Category c, Guid userId) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Color = c.Color,
        Icon = c.Icon,
        IsDefault = c.IsDefault,
        IsOwner = c.UserId == userId
    };
}