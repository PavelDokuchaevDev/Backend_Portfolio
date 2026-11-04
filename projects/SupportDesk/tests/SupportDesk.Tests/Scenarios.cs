using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;
using Xunit;

namespace SupportDesk.Tests;

public sealed class Scenarios
{
    [Fact]
    public async Task Ticket_uses_database_policy_and_rejects_stale_version()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        var priority = Guid.NewGuid().ToString("N")[..20];
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/policies", new CreatePolicy(priority, 90))).StatusCode);
        var response = await client.PostAsJsonAsync("/api/tickets", new CreateTicket("Cannot sign in", priority));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = (await response.Content.ReadFromJsonAsync<TicketView>())!;
        Assert.InRange(ticket.DueAt, DateTimeOffset.UtcNow.AddMinutes(89), DateTimeOffset.UtcNow.AddMinutes(91));
        var changed = await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status", new ChangeStatus("InProgress", ticket.Version));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var current = (await changed.Content.ReadFromJsonAsync<TicketView>())!;
        Assert.NotEqual(ticket.Version, current.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status", new ChangeStatus("Resolved", ticket.Version))).StatusCode);
        var resolved = await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status", new ChangeStatus("Resolved", current.Version));
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        Assert.NotNull((await resolved.Content.ReadFromJsonAsync<TicketView>())!.ResolvedAt);
    }

    [Fact]
    public async Task Unconfigured_priority_returns_validation_error()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/tickets", new CreateTicket("Question", "unknown"))).StatusCode);
    }

    [Fact]
    public void Sla_calculation_is_deterministic_and_status_cannot_skip_steps()
    {
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        var ticket = new Ticket("Cannot sign in", new SlaPolicy("high", 30), now);
        Assert.Equal(now.AddMinutes(30), ticket.DueAt);
        Assert.Throws<ConflictException>(() => ticket.Transition("Resolved", ticket.Version, now));
        ticket.Transition("InProgress", ticket.Version, now);
        ticket.Transition("Resolved", ticket.Version, now.AddMinutes(10));
        Assert.Equal(now.AddMinutes(10), ticket.ResolvedAt);
    }

    [Fact]
    public async Task Ef_concurrency_token_prevents_lost_updates()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticket = new Ticket("Question", new SlaPolicy("high", 30), DateTimeOffset.UtcNow);
        first.Tickets.Add(ticket);
        await first.SaveChangesAsync();
        var stale = await second.Tickets.SingleAsync(x => x.Id == ticket.Id);
        ticket.Transition("InProgress", ticket.Version, DateTimeOffset.UtcNow);
        await first.SaveChangesAsync();
        stale.Transition("InProgress", stale.Version, DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Overdue_query_excludes_resolved_tickets()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var policy = new SlaPolicy("test", 1);
        var late = new Ticket("Late", policy, DateTimeOffset.UtcNow.AddHours(-1));
        var resolved = new Ticket("Resolved", policy, DateTimeOffset.UtcNow.AddHours(-1));
        resolved.Transition("InProgress", resolved.Version, DateTimeOffset.UtcNow);
        resolved.Transition("Resolved", resolved.Version, DateTimeOffset.UtcNow);
        db.Tickets.AddRange(late, resolved, new Ticket("Fresh", new SlaPolicy("test", 60), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        var rows = (await client.GetFromJsonAsync<TicketView[]>("/api/tickets/overdue"))!;
        Assert.Contains(rows, x => x.Id == late.Id);
        Assert.DoesNotContain(rows, x => x.Id == resolved.Id);
    }
}
