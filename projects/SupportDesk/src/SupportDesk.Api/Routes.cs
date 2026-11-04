using SupportDesk.Application;
public static class Routes
{
    public static void MapRoutes(this WebApplication app)
    {
        app.MapGet("/api/policies", async (AppService service, CancellationToken ct) => Results.Ok(await service.Policies(ct))).WithTags("Policies");
        app.MapPost("/api/policies", async (CreatePolicy request, AppService service, CancellationToken ct) =>
            Results.Ok(await service.Create(request, ct))).WithTags("Policies");
        var tickets = app.MapGroup("/api/tickets").WithTags("Tickets");
        tickets.MapGet("/overdue", async (AppService service, CancellationToken ct) => Results.Ok(await service.Overdue(ct)));
        tickets.MapGet("/{id:guid}", async (Guid id, AppService service, CancellationToken ct) => Results.Ok(await service.Ticket(id, ct)));
        tickets.MapPost("/", async (CreateTicket request, AppService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return Results.Created($"/api/tickets/{result.Id}", result);
        });
        tickets.MapPatch("/{id:guid}/status", async (Guid id, ChangeStatus request, AppService service, CancellationToken ct) => Results.Ok(await service.Transition(id, request, ct)));
    }
}
