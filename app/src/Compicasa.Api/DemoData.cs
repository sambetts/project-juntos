using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// Loads the made-up people (D-029): the 14 hand-written profiles, always, and one generated batch,
/// replaced each day after <see cref="RefreshHour"/> Madrid time. Anything that involves a replaced
/// profile (likes either way, groups) goes with it.
/// </summary>
public sealed class DemoData(AppDb db, TimeProvider clock, ILogger<DemoData> log)
{
    public const int RefreshHour = 4;
    static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    /// <summary>The batch that should be loaded now: today's after 04:00 Madrid time, yesterday's before.</summary>
    public static DateOnly DueBatch(TimeProvider clock)
    {
        var madrid = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Madrid);
        return DateOnly.FromDateTime(madrid.AddHours(-RefreshHour).DateTime);
    }

    public async Task EnsureSeedsAsync(CancellationToken ct = default)
    {
        var existing = await db.Accounts.Where(a => a.Kind == AccountKind.Seed).ToDictionaryAsync(a => a.Id, ct);
        foreach (var p in Seeds.Profiles())
        {
            if (!existing.TryGetValue(p.Id, out var acc))
            {
                acc = new Account { Id = p.Id, Kind = AccountKind.Seed, CreatedAt = clock.GetUtcNow() };
                db.Accounts.Add(acc);
            }
            acc.WriteProfile(p); // keep them in step with seed-profiles.json
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Loads the due batch if it isn't loaded yet. Returns true if it changed anything.</summary>
    public async Task<bool> RefreshIfDueAsync(CancellationToken ct = default) => await RefreshAsync(DueBatch(clock), force: false, ct);

    public async Task<bool> RefreshAsync(DateOnly day, bool force, CancellationToken ct = default)
    {
        var state = await db.DemoStates.FindAsync([1], ct);
        if (state is not null && state.BatchDate == day && !force) return false;

        var prefix = DemoGenerator.Prefix(day);
        var stale = await db.Accounts.Where(a => a.Kind == AccountKind.Generated && !a.Id.StartsWith(prefix)).Select(a => a.Id).ToListAsync(ct);
        if (stale.Count > 0)
        {
            await db.Reactions.Where(r => stale.Contains(r.FromId) || stale.Contains(r.ToId)).ExecuteDeleteAsync(ct);
            var groupIds = await db.GroupMembers.Where(m => stale.Contains(m.AccountId)).Select(m => m.GroupId).Distinct().ToListAsync(ct);
            await db.GroupMembers.Where(m => groupIds.Contains(m.GroupId)).ExecuteDeleteAsync(ct);
            await db.Groups.Where(g => groupIds.Contains(g.Id)).ExecuteDeleteAsync(ct);
            await db.Accounts.Where(a => stale.Contains(a.Id)).ExecuteDeleteAsync(ct);
        }

        var have = await db.Accounts.Where(a => a.Id.StartsWith(prefix)).Select(a => a.Id).ToListAsync(ct);
        foreach (var p in DemoGenerator.Batch(day).Where(p => !have.Contains(p.Id)))
        {
            var acc = new Account { Id = p.Id, Kind = AccountKind.Generated, CreatedAt = clock.GetUtcNow() };
            acc.WriteProfile(p);
            db.Accounts.Add(acc);
        }
        if (state is null) db.DemoStates.Add(state = new DemoState());
        state.BatchDate = day;
        state.RefreshedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        log.LogInformation("Made-up batch {Day} loaded; {Removed} old made-up profiles removed", day, stale.Count);
        return true;
    }
}

/// <summary>Checks every 15 minutes whether a new day's batch is due, so a restart or a missed hour can't skip a day.</summary>
public sealed class DemoRefreshWorker(IServiceScopeFactory scopes, ILogger<DemoRefreshWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DemoData>().RefreshIfDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Daily refresh of the made-up profiles failed; will retry");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
