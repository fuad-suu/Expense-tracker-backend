using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Income> Incomes => Set<Income>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<User>(e =>
    {
        e.HasKey(u => u.Id);
        e.HasIndex(u => u.Email).IsUnique();
    });

    modelBuilder.Entity<Expense>(e =>
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Amount).HasPrecision(18, 2);

        e.HasOne(x => x.User)
         .WithMany(u => u.Expenses)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Category)
         .WithMany(c => c.Expenses)
         .HasForeignKey(x => x.CategoryId)
         .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Category>(e =>
    {
        e.HasKey(c => c.Id);

        e.HasOne(c => c.User)
         .WithMany(u => u.Categories)
         .HasForeignKey(c => c.UserId)
         .IsRequired(false)
         .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<Budget>(e =>
    {
        e.HasKey(b => b.Id);
        e.Property(b => b.LimitAmount).HasPrecision(18, 2);

        e.HasOne(b => b.User)
         .WithMany(u => u.Budgets)
         .HasForeignKey(b => b.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);

            e.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Income>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Amount).HasPrecision(18, 2);

            e.HasOne(i => i.User)
             .WithMany(u => u.Incomes)
             .HasForeignKey(i => i.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

    SeedDefaultCategories(modelBuilder);
}

    private static void SeedDefaultCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Food & Dining",    Color = "#f97316", Icon = "utensils",      IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Transport",        Color = "#3b82f6", Icon = "car",           IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Housing",          Color = "#8b5cf6", Icon = "home",          IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Health",           Color = "#22c55e", Icon = "heart-pulse",   IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Entertainment",    Color = "#ec4899", Icon = "clapperboard",  IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Shopping",         Color = "#eab308", Icon = "shopping-bag",  IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Education",        Color = "#06b6d4", Icon = "book-open",     IsDefault = true },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "Other",            Color = "#6b7280", Icon = "ellipsis",      IsDefault = true }
        );
    }
}