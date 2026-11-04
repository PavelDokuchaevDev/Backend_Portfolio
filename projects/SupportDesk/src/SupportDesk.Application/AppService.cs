using SupportDesk.Domain;
namespace SupportDesk.Application;
public record CreatePolicy(string Priority, int ResolutionMinutes);
public record CreateTicket(string Title, string Priority);
public record ChangeStatus(string Status, Guid Version);
public record PolicyView(Guid Id, string Priority, int ResolutionMinutes);
public record TicketView(Guid Id, string Title, string Priority, string Status, DateTimeOffset DueAt, DateTimeOffset? ResolvedAt, Guid Version);
public interface IStore
{
    Task AddPolicy(SlaPolicy policy, CancellationToken ct);
    Task<IReadOnlyList<PolicyView>> Policies(CancellationToken ct);
    Task<SlaPolicy?> Policy(string priority, CancellationToken ct);
    Task AddTicket(Ticket ticket, CancellationToken ct);
    Task<Ticket?> Ticket(Guid id, CancellationToken ct);
    Task Save(CancellationToken ct);
    Task<IReadOnlyList<TicketView>> Overdue(DateTimeOffset now, CancellationToken ct);
}
public sealed class AppService(IStore store, TimeProvider clock)
{
    public async Task<PolicyView> Create(CreatePolicy request, CancellationToken ct)
    {
        var p = new SlaPolicy(request.Priority, request.ResolutionMinutes);
        await store.AddPolicy(p, ct);
        return new(p.Id, p.Priority, p.ResolutionMinutes);
    }
    public Task<IReadOnlyList<PolicyView>> Policies(CancellationToken ct) => store.Policies(ct);
    public async Task<TicketView> Create(CreateTicket request, CancellationToken ct)
    {
        var priority = Guard.Text(request.Priority, nameof(request.Priority), 30).ToLowerInvariant();
        var policy = await store.Policy(priority, ct) ?? throw new ValidationException("Create an SLA policy for this priority first.");
        var ticket = new Ticket(request.Title, policy, clock.GetUtcNow());
        await store.AddTicket(ticket, ct);
        return View(ticket);
    }
    public async Task<TicketView> Ticket(Guid id, CancellationToken ct) => View(await store.Ticket(id, ct) ?? throw new NotFoundException("Ticket not found."));
    public async Task<TicketView> Transition(Guid id, ChangeStatus request, CancellationToken ct)
    {
        var ticket = await store.Ticket(id, ct) ?? throw new NotFoundException("Ticket not found.");
        ticket.Transition(request.Status, request.Version, clock.GetUtcNow());
        await store.Save(ct);
        return View(ticket);
    }
    public Task<IReadOnlyList<TicketView>> Overdue(CancellationToken ct) => store.Overdue(clock.GetUtcNow(), ct);
    private static TicketView View(Ticket t) => new(t.Id, t.Title, t.Priority, t.Status, t.DueAt, t.ResolvedAt, t.Version);
}
