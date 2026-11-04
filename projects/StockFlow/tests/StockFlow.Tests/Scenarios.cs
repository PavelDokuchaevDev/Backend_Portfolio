using System.Net;
using System.Net.Http.Json;
using StockFlow.Application;
using StockFlow.Domain;
using Xunit;

namespace StockFlow.Tests;

public sealed class Scenarios
{
    [Fact]
    public async Task Order_reserves_stock_and_cancellation_restores_it_only_once()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        var product = await CreateProduct(client, 5);
        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrder(product.Id, 3));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderView>())!;
        Assert.Equal(37.50m, order.Total);
        Assert.Equal(2, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Stock);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/orders", new PlaceOrder(product.Id, 3))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/orders/{order.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/orders/{order.Id}/cancel", null)).StatusCode);
        Assert.Equal(5, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Stock);
        Assert.Equal("Cancelled", (await client.GetFromJsonAsync<OrderView>($"/api/orders/{order.Id}"))!.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Invalid_quantity_does_not_change_stock(int quantity)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        var product = await CreateProduct(client, 5);
        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrder(product.Id, quantity));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(5, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Stock);
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/orders", new PlaceOrder(Guid.NewGuid(), 1))).StatusCode);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.001)]
    public void Money_rejects_negative_or_fractional_cents(decimal price) =>
        Assert.Throws<ValidationException>(() => new Product("Keyboard", price, 1));

    [PostgresFact]
    public async Task Postgres_parallel_orders_cannot_oversell()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        var product = await CreateProduct(client, 1);
        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PostAsJsonAsync("/api/orders", new PlaceOrder(product.Id, 1))));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Stock);
    }

    private static async Task<ProductView> CreateProduct(HttpClient client, int stock)
    {
        var response = await client.PostAsJsonAsync("/api/products", new CreateProduct("Keyboard", 12.50m, stock));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductView>())!;
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("PORTFOLIO_TEST_DATABASE") is null)
            Skip = "Requires PostgreSQL; executed by the PostgreSQL CI job.";
    }
}
