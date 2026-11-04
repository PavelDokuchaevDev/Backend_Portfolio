using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database is required.")));
builder.Services.AddScoped<IStore, EfStore>();
builder.Services.AddScoped<AppService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" })).ExcludeFromDescription();
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new
    {
        status = "ready"
    })
        : Results.StatusCode(503)).ExcludeFromDescription();
app.MapRoutes();
app.Run();

public partial class Program;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var status = error switch
        {
            ValidationException => 400,
            NotFoundException => 404,
            ConflictException or DbUpdateConcurrencyException => 409,
            DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" } } => 409,
            _ => 500
        };
        if (status == 500)
            logger.LogError(error, "Unhandled request failure");
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Validation failed", 404 => "Not found", 409 => "Conflict", _ => "Server error" },
            Detail = status == 500 ? "An unexpected error occurred." : status == 409 ? "The operation conflicts with existing data or a concurrent change." : error.Message,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: ct);
        return true;
    }
}
