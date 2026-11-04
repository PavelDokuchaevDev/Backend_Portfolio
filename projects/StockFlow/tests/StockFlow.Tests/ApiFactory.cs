using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockFlow.Infrastructure;

namespace StockFlow.Tests;
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                var postgres = Environment.GetEnvironmentVariable("PORTFOLIO_TEST_DATABASE");
                if (postgres is null)
                    options.UseSqlite(connection).ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCustomizer, SqliteModelCustomizer>();
                else
                    options.UseNpgsql(postgres);
            });
        });
    }
    public HttpClient CreateInitializedClient()
    {
        var client = CreateClient();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (Environment.GetEnvironmentVariable("PORTFOLIO_TEST_DATABASE") is null)
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
        return client;
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            connection.Dispose();
    }
}

// SQLite has no native decimal SUM or DateTimeOffset ordering. Keep the domain and
// production PostgreSQL model intact; adapt storage only inside the test host.
public sealed class SqliteModelCustomizer(Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizerDependencies dependencies)
    : Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(decimal))
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal, long>(v => (long)(v * 100m), v => v / 100m));
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero)));
            }
    }
}
