namespace SupportDesk.Domain;
public sealed class SlaPolicy
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Priority { get; private set; } = "";
    public int ResolutionMinutes
    {
        get; private set;
    }
    private SlaPolicy()
    {
    }
    public SlaPolicy(string priority, int resolutionMinutes)
    {
        Priority = Guard.Text(priority, nameof(priority), 30).ToLowerInvariant();
        if (resolutionMinutes is < 1 or > 525600)
            throw new ValidationException("ResolutionMinutes must be 1..525600.");
        ResolutionMinutes = resolutionMinutes;
    }
}
public sealed class Ticket
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = "";
    public string Priority { get; private set; } = "";
    public string Status { get; private set; } = "Open";
    public DateTimeOffset CreatedAt
    {
        get; private set;
    }
    public DateTimeOffset DueAt
    {
        get; private set;
    }
    public DateTimeOffset? ResolvedAt
    {
        get; private set;
    }
    public Guid Version { get; private set; } = Guid.NewGuid();
    private Ticket()
    {
    }
    public Ticket(string title, SlaPolicy policy, DateTimeOffset now)
    {
        Title = Guard.Text(title, nameof(title));
        Priority = policy.Priority;
        CreatedAt = now;
        DueAt = now.AddMinutes(policy.ResolutionMinutes);
    }
    public void Transition(string? next, Guid expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new ConflictException("Ticket was changed. Read its current version.");
        if (!(Status == "Open" && next == "InProgress" || Status == "InProgress" && next == "Resolved"))
            throw new ConflictException("Allowed transitions: Open -> InProgress -> Resolved.");
        Status = next;
        Version = Guid.NewGuid();
        if (next == "Resolved")
            ResolvedAt = now;
    }
}
