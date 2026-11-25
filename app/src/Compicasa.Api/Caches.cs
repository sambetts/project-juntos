using System.Collections.Concurrent;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// The made-up people, read and decoded once per daily batch rather than on every request (Session 32 code review):
/// nearly every signed-in call, including the 10- and 30-second polls, needs everyone the viewer could meet. They
/// change only when a batch is loaded, which stamps the DemoState row, and when the app starts (seed profiles), so
/// that one small row says whether the copy is current. Kept per database, so test databases never mix.
/// The profiles are shared between requests: nothing may change them (Matching only reads).
/// </summary>
public static class DemoProfiles
{
    sealed record Snapshot(DateOnly Batch, DateTimeOffset At, IReadOnlyList<(Account Account, Profile Profile)> People);
    static readonly ConcurrentDictionary<string, Snapshot> Cache = new();

    public static async Task<IReadOnlyList<(Account Account, Profile Profile)>> GetAsync(AppDb db, CancellationToken ct)
    {
        var key = db.Database.GetConnectionString() ?? "";
        var stamp = await db.DemoStates.AsNoTracking().Where(s => s.Id == 1).Select(s => new { s.BatchDate, s.RefreshedAt }).FirstOrDefaultAsync(ct);
        if (stamp is not null && Cache.TryGetValue(key, out var snap) && snap.Batch == stamp.BatchDate && snap.At == stamp.RefreshedAt)
            return snap.People;
        var people = (await db.Accounts.AsNoTracking().Where(a => a.Kind != AccountKind.User).ToListAsync(ct))
            .Select(a => (a, a.ReadProfile())).ToList();
        if (stamp is not null) Cache[key] = new Snapshot(stamp.BatchDate, stamp.RefreshedAt, people);
        return people;
    }
}

/// <summary>
/// Every home still advertised, held in memory for a few minutes (Session 32 code review) instead of read in full
/// on each browse, filter change and group's homes. The index changes once a day, when the crawl runs, so a copy up to
/// <c>Homes:CacheSeconds</c> old (300 by default; 0 turns it off) is current enough. Stopped agencies and made-up
/// homes are filtered on each call, so changing those settings applies at once. Kept per database.
/// </summary>
public static class HomeIndex
{
    public sealed record Entry(Home Home, HomeListing Listing);
    sealed record Snapshot(DateTimeOffset At, IReadOnlyList<Entry> Homes);
    static readonly ConcurrentDictionary<string, Snapshot> Cache = new();
    static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<List<Entry>> LiveAsync(AppDb db, IConfiguration cfg, TimeProvider clock, CancellationToken ct)
    {
        var all = await AllAsync(db, cfg, clock, ct);
        var stopped = HomeSources.Stopped(cfg);
        var demo = DemoHomes.Enabled(cfg);
        return all.Where(e => !stopped.Contains(e.Home.Source) && (demo || !e.Home.IsDemo)).ToList();
    }

    static async Task<IReadOnlyList<Entry>> AllAsync(AppDb db, IConfiguration cfg, TimeProvider clock, CancellationToken ct)
    {
        var ttl = TimeSpan.FromSeconds(cfg.GetValue("Homes:CacheSeconds", 300));
        var key = db.Database.GetConnectionString() ?? "";
        bool Fresh(out IReadOnlyList<Entry> homes)
        {
            homes = [];
            if (ttl <= TimeSpan.Zero || !Cache.TryGetValue(key, out var s) || clock.GetUtcNow() - s.At >= ttl) return false;
            homes = s.Homes;
            return true;
        }
        if (Fresh(out var cached)) return cached;
        // One reader at a time, so a burst of requests after the copy expires reads the table once.
        await Gate.WaitAsync(ct);
        try
        {
            if (Fresh(out cached)) return cached;
            var rows = await db.Homes.AsNoTracking().Where(h => h.GoneAt == null).ToListAsync(ct);
            var homes = rows.Select(h => new Entry(h, h.ToListing())).ToList();
            if (ttl > TimeSpan.Zero) Cache[key] = new Snapshot(clock.GetUtcNow(), homes);
            return homes;
        }
        finally { Gate.Release(); }
    }
}
