using StockFlow.Application;
public static class Routes
{
    public static void MapRoutes(this WebApplication app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products");
        products.MapGet("/", async (AppService service, CancellationToken ct, int page = 1) => Results.Ok(await service.Products(page, ct)));
        products.MapGet("/{id:guid}", async (Guid id, AppService service, CancellationToken ct) => Results.Ok(await service.Product(id, ct)));
        products.MapPost("/", async (CreateProduct request, AppService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return Results.Created($"/api/products/{result.Id}", result);
        });
        var orders = app.MapGroup("/api/orders").WithTags("Orders");
        orders.MapGet("/{id:guid}", async (Guid id, AppService service, CancellationToken ct) => Results.Ok(await service.Order(id, ct)));
        orders.MapPost("/", async (PlaceOrder request, AppService service, CancellationToken ct) =>
        {
            var result = await service.Place(request, ct);
            return Results.Created($"/api/orders/{result.Id}", result);
        });
        orders.MapPost("/{id:guid}/cancel", async (Guid id, AppService service, CancellationToken ct) => { await service.Cancel(id, ct); return Results.NoContent(); });
    }
}
