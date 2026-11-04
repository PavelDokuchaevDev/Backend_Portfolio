using ExpenseLens.Application;
public static class Routes
{
    public static void MapRoutes(this WebApplication app)
    {
        var categories = app.MapGroup("/api/categories").WithTags("Categories");
        categories.MapGet("/", async (AppService service, CancellationToken ct) => Results.Ok(await service.Categories(ct)));
        categories.MapGet("/{id:guid}", async (Guid id, AppService service, CancellationToken ct) => Results.Ok(await service.Category(id, ct)));
        categories.MapPost("/", async (CreateCategory request, AppService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return Results.Created($"/api/categories/{result.Id}", result);
        });
        app.MapGet("/api/expenses/{id:guid}", async (Guid id, AppService service, CancellationToken ct) => Results.Ok(await service.Expense(id, ct))).WithTags("Expenses");
        app.MapPost("/api/expenses", async (CreateExpense request, AppService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return Results.Created($"/api/expenses/{result.Id}", result);
        }).WithTags("Expenses");
        app.MapGet("/api/reports/monthly", async (int year, int month, AppService service, CancellationToken ct) => Results.Ok(await service.Report(year, month, ct))).WithTags("Reports");
    }
}
