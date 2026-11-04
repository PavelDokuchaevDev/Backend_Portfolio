namespace ExpenseLens.Domain;
public sealed class Category
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public decimal MonthlyBudget
    {
        get; private set;
    }
    private Category()
    {
    }
    public Category(string name, decimal monthlyBudget)
    {
        Name = Guard.Text(name, nameof(name));
        MonthlyBudget = Guard.Money(monthlyBudget, nameof(monthlyBudget));
    }
}
public sealed class Expense
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CategoryId
    {
        get; private set;
    }
    public decimal Amount
    {
        get; private set;
    }
    public DateOnly Date
    {
        get; private set;
    }
    public string Description { get; private set; } = "";
    private Expense()
    {
    }
    public Expense(Guid categoryId, decimal amount, DateOnly date, string description)
    {
        if (date == default)
            throw new ValidationException("Date is required.");
        CategoryId = categoryId;
        Amount = Guard.Money(amount, nameof(amount));
        Date = date;
        Description = Guard.Text(description, nameof(description), 300);
    }
}
