using ExpenseLens.Domain;
namespace ExpenseLens.Application;
public record CreateCategory(string Name, decimal MonthlyBudget);
public record CreateExpense(Guid CategoryId, decimal Amount, DateOnly Date, string Description);
public record CategoryView(Guid Id, string Name, decimal MonthlyBudget);
public record ExpenseView(Guid Id, Guid CategoryId, decimal Amount, DateOnly Date, string Description);
public record BudgetRow(Guid CategoryId, string Name, decimal Budget, decimal Spent)
{
    public decimal Remaining => Budget - Spent;
    public bool OverBudget => Spent > Budget;
}
public interface IStore
{
    Task AddCategory(Category category, CancellationToken ct);
    Task<Category?> Category(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CategoryView>> Categories(CancellationToken ct);
    Task AddExpense(Expense expense, CancellationToken ct);
    Task<Expense?> Expense(Guid id, CancellationToken ct);
    Task<IReadOnlyList<BudgetRow>> Report(DateOnly start, DateOnly end, CancellationToken ct);
}
public sealed class AppService(IStore store)
{
    public async Task<CategoryView> Create(CreateCategory request, CancellationToken ct)
    {
        var category = new Category(request.Name, request.MonthlyBudget);
        await store.AddCategory(category, ct);
        return new(category.Id, category.Name, category.MonthlyBudget);
    }
    public Task<IReadOnlyList<CategoryView>> Categories(CancellationToken ct) => store.Categories(ct);
    public async Task<CategoryView> Category(Guid id, CancellationToken ct)
    {
        var c = await store.Category(id, ct) ?? throw new NotFoundException("Category not found.");
        return new(c.Id, c.Name, c.MonthlyBudget);
    }
    public async Task<ExpenseView> Create(CreateExpense request, CancellationToken ct)
    {
        _ = await store.Category(request.CategoryId, ct) ?? throw new NotFoundException("Category not found.");
        var expense = new Expense(request.CategoryId, request.Amount, request.Date, request.Description);
        await store.AddExpense(expense, ct);
        return View(expense);
    }
    public async Task<ExpenseView> Expense(Guid id, CancellationToken ct) => View(await store.Expense(id, ct) ?? throw new NotFoundException("Expense not found."));
    public Task<IReadOnlyList<BudgetRow>> Report(int year, int month, CancellationToken ct)
    {
        if (year is < 2000 or > 9998 || month is < 1 or > 12)
            throw new ValidationException("Year must be 2000..9998 and month 1..12.");
        var start = new DateOnly(year, month, 1);
        return store.Report(start, start.AddMonths(1), ct);
    }
    private static ExpenseView View(Expense e) => new(e.Id, e.CategoryId, e.Amount, e.Date, e.Description);
}
