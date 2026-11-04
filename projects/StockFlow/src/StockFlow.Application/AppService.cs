using StockFlow.Domain;
namespace StockFlow.Application;
public record CreateProduct(string Name, decimal Price, int Stock);
public record PlaceOrder(Guid ProductId, int Quantity);
public record ProductView(Guid Id, string Name, decimal Price, int Stock);
public record OrderView(Guid Id, Guid ProductId, int Quantity, decimal Total, string Status);
public interface IStore
{
    Task AddProduct(Product product, CancellationToken ct);
    Task<IReadOnlyList<Product>> Products(int page, CancellationToken ct);
    Task<Product?> Product(Guid id, CancellationToken ct);
    Task<Order?> FindOrder(Guid id, CancellationToken ct);
    Task Reserve(Order order, CancellationToken ct);
    Task Cancel(Guid id, CancellationToken ct);
}
public sealed class AppService(IStore store, TimeProvider clock)
{
    public async Task<ProductView> Create(CreateProduct request, CancellationToken ct)
    {
        var product = new Product(request.Name, request.Price, request.Stock);
        await store.AddProduct(product, ct);
        return View(product);
    }
    public async Task<IReadOnlyList<ProductView>> Products(int page, CancellationToken ct)
    {
        if (page < 1 || page > 10000)
            throw new ValidationException("Page must be between 1 and 10000.");
        return (await store.Products(page, ct)).Select(View).ToArray();
    }
    public async Task<ProductView> Product(Guid id, CancellationToken ct) => View(await store.Product(id, ct) ?? throw new NotFoundException("Product not found."));
    public async Task<OrderView> Order(Guid id, CancellationToken ct) => View(await store.FindOrder(id, ct) ?? throw new NotFoundException("Order not found."));
    public async Task<OrderView> Place(PlaceOrder request, CancellationToken ct)
    {
        var product = await store.Product(request.ProductId, ct) ?? throw new NotFoundException("Product not found.");
        var order = new Order(product, request.Quantity, clock.GetUtcNow());
        await store.Reserve(order, ct);
        return View(order);
    }
    public Task Cancel(Guid id, CancellationToken ct) => store.Cancel(id, ct);
    private static ProductView View(Product p) => new(p.Id, p.Name, p.Price, p.Stock);
    private static OrderView View(Order o) => new(o.Id, o.ProductId, o.Quantity, o.Total, o.Status);
}
