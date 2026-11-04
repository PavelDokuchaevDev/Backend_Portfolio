namespace StockFlow.Domain;
public sealed class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public decimal Price
    {
        get; private set;
    }
    public int Stock
    {
        get; private set;
    }
    private Product()
    {
    }
    public Product(string name, decimal price, int stock)
    {
        Name = Guard.Text(name, nameof(name));
        Price = Guard.Money(price, nameof(price));
        if (stock < 0 || stock > 1_000_000)
            throw new ValidationException("Stock must be between 0 and 1000000.");
        Stock = stock;
    }
}
public sealed class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId
    {
        get; private set;
    }
    public int Quantity
    {
        get; private set;
    }
    public decimal Total
    {
        get; private set;
    }
    public string Status { get; private set; } = "Confirmed";
    public DateTimeOffset CreatedAt
    {
        get; private set;
    }
    private Order()
    {
    }
    public Order(Product product, int quantity, DateTimeOffset now)
    {
        if (quantity <= 0 || quantity > 1_000_000)
            throw new ValidationException("Quantity must be between 1 and 1000000.");
        ProductId = product.Id;
        Quantity = quantity;
        Total = product.Price * quantity;
        CreatedAt = now;
    }
}
