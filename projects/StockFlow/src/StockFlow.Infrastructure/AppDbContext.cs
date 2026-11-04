using Microsoft.EntityFrameworkCore;
using StockFlow.Application;
using StockFlow.Domain;
namespace StockFlow.Infrastructure;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Product>().Property(x => x.Name).HasMaxLength(120);
        model.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        model.Entity<Product>().ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "\"Stock\" >= 0"));
        model.Entity<Order>().Property(x => x.Total).HasPrecision(18, 2);
        model.Entity<Order>().Property(x => x.Status).HasMaxLength(20);
        model.Entity<Order>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Order>().ToTable(t => t.HasCheckConstraint("CK_Order_Quantity", "\"Quantity\" > 0"));
    }
}
public sealed class EfStore(AppDbContext db) : IStore
{
    public async Task AddProduct(Product product, CancellationToken ct)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<Product>> Products(int page, CancellationToken ct) => await db.Products.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * 20).Take(20).ToListAsync(ct);
    public Task<Product?> Product(Guid id, CancellationToken ct) => db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Order?> FindOrder(Guid id, CancellationToken ct) => db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task Reserve(Order order, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var changed = await db.Products.Where(x => x.Id == order.ProductId && x.Stock >= order.Quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock - order.Quantity), ct);
        if (changed == 0)
            throw new ConflictException("Insufficient stock.");
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task Cancel(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await FindOrder(id, ct) ?? throw new NotFoundException("Order not found.");
        var changed = await db.Orders.Where(x => x.Id == id && x.Status == "Confirmed")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Cancelled"), ct);
        if (changed == 0)
            throw new ConflictException("Order was already cancelled.");
        await db.Products.Where(x => x.Id == order.ProductId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock + order.Quantity), ct);
        await tx.CommitAsync(ct);
    }
}
