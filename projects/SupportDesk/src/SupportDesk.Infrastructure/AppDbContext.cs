using Microsoft.EntityFrameworkCore;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SlaPolicy> Policies => Set<SlaPolicy>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SlaPolicy>().Property(x => x.Priority).HasMaxLength(30);
        model.Entity<SlaPolicy>().HasIndex(x => x.Priority).IsUnique();
        model.Entity<Ticket>().Property(x => x.Title).HasMaxLength(120);
        model.Entity<Ticket>().Property(x => x.Priority).HasMaxLength(30);
        model.Entity<Ticket>().Property(x => x.Status).HasMaxLength(20);
        model.Entity<Ticket>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<Ticket>().HasIndex(x => new { x.Status, x.DueAt });
    }
}
public sealed class EfStore(AppDbContext db) : IStore
{
    public async Task AddPolicy(SlaPolicy policy, CancellationToken ct)
    {
        db.Policies.Add(policy);
        await db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<PolicyView>> Policies(CancellationToken ct) => await db.Policies.AsNoTracking().OrderBy(x => x.Priority).Select(x => new PolicyView(x.Id, x.Priority, x.ResolutionMinutes)).ToListAsync(ct);
    public Task<SlaPolicy?> Policy(string priority, CancellationToken ct) => db.Policies.AsNoTracking().SingleOrDefaultAsync(x => x.Priority == priority, ct);
    public async Task AddTicket(Ticket ticket, CancellationToken ct)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
    }
    public Task<Ticket?> Ticket(Guid id, CancellationToken ct) => db.Tickets.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public async Task<IReadOnlyList<TicketView>> Overdue(DateTimeOffset now, CancellationToken ct) => await db.Tickets.AsNoTracking()
        .Where(x => x.Status != "Resolved" && x.DueAt < now).OrderBy(x => x.DueAt).Take(100)
        .Select(x => new TicketView(x.Id, x.Title, x.Priority, x.Status, x.DueAt, x.ResolvedAt, x.Version)).ToListAsync(ct);
}
