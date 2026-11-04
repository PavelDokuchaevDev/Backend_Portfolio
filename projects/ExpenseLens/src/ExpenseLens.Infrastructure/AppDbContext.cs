using ExpenseLens.Application;
using ExpenseLens.Domain;
using Microsoft.EntityFrameworkCore;
namespace ExpenseLens.Infrastructure;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Category>().Property(x => x.Name).HasMaxLength(120);
        model.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        model.Entity<Category>().Property(x => x.MonthlyBudget).HasPrecision(18, 2);
        model.Entity<Expense>().Property(x => x.Amount).HasPrecision(18, 2);
        model.Entity<Expense>().Property(x => x.Description).HasMaxLength(300);
        model.Entity<Expense>().HasIndex(x => new { x.CategoryId, x.Date });
        model.Entity<Expense>().HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Expense>().ToTable(t => t.HasCheckConstraint("CK_Expense_Amount", "\"Amount\" > 0"));
    }
}
public sealed class EfStore(AppDbContext db) : IStore
{
    public async Task AddCategory(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
    }
    public Task<Category?> Category(Guid id, CancellationToken ct) => db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<CategoryView>> Categories(CancellationToken ct) => await db.Categories.AsNoTracking().OrderBy(x => x.Name).Select(x => new CategoryView(x.Id, x.Name, x.MonthlyBudget)).ToListAsync(ct);
    public async Task AddExpense(Expense expense, CancellationToken ct)
    {
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
    }
    public Task<Expense?> Expense(Guid id, CancellationToken ct) => db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<BudgetRow>> Report(DateOnly start, DateOnly end, CancellationToken ct) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => new BudgetRow(c.Id, c.Name, c.MonthlyBudget,
            db.Expenses.Where(e => e.CategoryId == c.Id && e.Date >= start && e.Date < end).Sum(e => (decimal?)e.Amount) ?? 0)).ToListAsync(ct);
}
