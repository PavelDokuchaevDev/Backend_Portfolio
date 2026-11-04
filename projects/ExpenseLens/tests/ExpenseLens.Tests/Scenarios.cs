using System.Net;
using System.Net.Http.Json;
using ExpenseLens.Application;
using ExpenseLens.Domain;
using Xunit;

namespace ExpenseLens.Tests;

public sealed class Scenarios
{
    [Fact]
    public async Task Monthly_report_includes_empty_categories_and_excludes_adjacent_months()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        var food = await Category(client, 100);
        var travel = await Category(client, 50);
        foreach (var (date, amount) in new[] { (new DateOnly(2026, 10, 31), 90m), (new DateOnly(2026, 11, 1), 60m), (new DateOnly(2026, 11, 30), 55m), (new DateOnly(2026, 12, 1), 90m) })
        {
            var result = await client.PostAsJsonAsync("/api/expenses", new CreateExpense(food.Id, amount, date, "Lunch"));
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(result.Headers.Location)).StatusCode);
        }
        var rows = (await client.GetFromJsonAsync<BudgetRow[]>("/api/reports/monthly?year=2026&month=11"))!;
        var foodRow = Assert.Single(rows, x => x.CategoryId == food.Id);
        Assert.Equal(115m, foodRow.Spent);
        Assert.Equal(-15m, foodRow.Remaining);
        Assert.True(foodRow.OverBudget);
        Assert.Equal(0, Assert.Single(rows, x => x.CategoryId == travel.Id).Spent);
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2026, 13)]
    public async Task Invalid_report_period_returns_400(int year, int month)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/reports/monthly?year={year}&month={month}")).StatusCode);
    }

    [Fact]
    public async Task Expense_for_unknown_category_returns_404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        var response = await client.PostAsJsonAsync("/api/expenses", new CreateExpense(Guid.NewGuid(), 10, new DateOnly(2026, 11, 1), "Lunch"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Invalid_amount_is_rejected(decimal amount) => Assert.Throws<ValidationException>(() =>
        new Expense(Guid.NewGuid(), amount, new DateOnly(2026, 11, 1), "Lunch"));

    private static async Task<CategoryView> Category(HttpClient client, decimal budget)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new CreateCategory(Guid.NewGuid().ToString(), budget));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryView>())!;
    }
}
