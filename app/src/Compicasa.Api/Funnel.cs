using System.Globalization;
using System.Security.Claims;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>Counts for one slice of real people (made-up people are never counted). Numbers only: no names, ids or answers.</summary>
public sealed record FunnelCounts(
    int SignedUp, int Onboarded, int LivingIn, int Letting, int Liked, int Matched,
    int Cohort14, int Matched14, int GroupsFormed, int GroupsShortlisting, int GroupsAgreedHome);

public sealed record FunnelWeek(string Week, int SignedUp, int Onboarded, int Matches);

public sealed record FunnelView(DateTimeOffset At, FunnelCounts All, FunnelCounts Madrid, IReadOnlyList<FunnelWeek> Weeks);

/// <summary>
/// The free release's funnel (B-074, `project/strategy.md` section 4), worked out from what the app already stores each time
/// it is asked: nothing extra is collected, and no tracking cookies are set. Only real accounts count, and a match or a
/// group counts only between real people, since made-up people say yes by simulation. Shown only to the accounts listed
/// in the setting <c>Admin:AccountIds</c> (comma-separated; an app setting, never in the repository); anyone else signed in gets
/// <c>{ "allowed": false }</c> and nothing more.
/// </summary>
public static class Funnel
{
    public const int MatchDays = 14;
    const int Weeks = 8;

    public static bool IsAdmin(IConfiguration cfg, string? accountId) =>
        accountId is not null && (cfg["Admin:AccountIds"] ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(accountId);

    /// <summary>The signed-in person is listed in <c>Admin:AccountIds</c> and is still a real account. Used by every admin page.</summary>
    public static async Task<bool> AllowedAsync(HttpContext http, AppDb db, IConfiguration cfg, CancellationToken ct)
    {
        var id = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return IsAdmin(cfg, id) && await db.Accounts.AnyAsync(a => a.Id == id && a.Kind == AccountKind.User, ct);
    }

    public static void Map(WebApplication app)
    {
        app.MapGet("/api/admin/funnel", async (HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock, CancellationToken ct) =>
        {
            if (!await AllowedAsync(http, db, cfg, ct)) return Results.Ok(new { allowed = false });
            return Results.Ok(new { allowed = true, funnel = await ComputeAsync(db, clock.GetUtcNow(), ct) });
        }).RequireAuthorization();
    }

    /// <summary>Madrid: finished their profile and chose at least one of Madrid's areas (an unfinished profile still has the default areas).</summary>
    sealed record Person(Account Account, Profile Profile, bool Madrid);

    public static async Task<FunnelView> ComputeAsync(AppDb db, DateTimeOffset now, CancellationToken ct = default)
    {
        // Small tables at this stage, so everything is compared in memory (SQLite can't compare DateTimeOffset values).
        var people = (await db.Accounts.Where(a => a.Kind == AccountKind.User).ToListAsync(ct))
            .Select(a => { var p = a.ReadProfile(); return new Person(a, p, a.OnboardedAt is not null && p.Areas.Any(MadridZones.Ids.Contains)); })
            .ToDictionary(x => x.Account.Id);
        var likes = (await db.Reactions.Where(r => r.Kind == ReactionKind.Like).ToListAsync(ct))
            .Where(r => people.ContainsKey(r.FromId)).ToList();
        var liked = likes.ToDictionary(r => (r.FromId, r.ToId), r => r.At);
        // A match between two real people, dated when the second like arrived.
        var matches = likes.Where(r => people.ContainsKey(r.ToId) && string.CompareOrdinal(r.FromId, r.ToId) < 0 && liked.ContainsKey((r.ToId, r.FromId)))
            .Select(r => (A: r.FromId, B: r.ToId, At: Max(r.At, liked[(r.ToId, r.FromId)]))).ToList();

        var groups = await db.Groups.Include(g => g.Members).ToListAsync(ct);
        var realGroups = groups.Where(g => Lifecycle.State(g) == "formed" && Lifecycle.Active(g).Count(m => people.ContainsKey(m.AccountId)) >= 2).ToList();
        var groupIds = realGroups.Select(g => g.Id).ToList();
        var candidates = await db.Candidates.Include(c => c.Votes).Where(c => groupIds.Contains(c.GroupId)).ToListAsync(ct);
        var homeIds = candidates.Select(c => c.HomeId).Distinct().ToList();
        var prices = await db.Homes.Where(h => homeIds.Contains(h.Id)).ToDictionaryAsync(h => h.Id, h => h.Price, ct);

        FunnelCounts Count(Func<Person, bool> keep)
        {
            var mine = people.Values.Where(keep).ToList();
            var ids = mine.Select(p => p.Account.Id).ToHashSet();
            var on = mine.Where(p => p.Account.OnboardedAt is not null).ToList();
            DateTimeOffset? FirstMatch(string id) => matches.Where(m => m.A == id || m.B == id).Select(m => (DateTimeOffset?)m.At).Min();
            var cohort = on.Where(p => p.Account.OnboardedAt!.Value.AddDays(MatchDays) <= now).ToList();
            var gs = realGroups.Where(g => ids.Contains(g.CreatedById)).ToList();
            bool Agreed(Group g) => candidates.Where(c => c.GroupId == g.Id)
                .Any(c => Shortlist.StageOf(c, prices.GetValueOrDefault(c.HomeId), Shortlist.Voters(g)) == Shortlist.Agreed);
            return new FunnelCounts(
                SignedUp: mine.Count,
                Onboarded: on.Count,
                LivingIn: on.Count(p => !p.Profile.IsLet()),
                Letting: on.Count(p => p.Profile.IsLet()),
                Liked: on.Count(p => likes.Any(r => r.FromId == p.Account.Id)),
                Matched: on.Count(p => FirstMatch(p.Account.Id) is not null),
                Cohort14: cohort.Count,
                Matched14: cohort.Count(p => FirstMatch(p.Account.Id) is { } at && at <= p.Account.OnboardedAt!.Value.AddDays(MatchDays)),
                GroupsFormed: gs.Count,
                GroupsShortlisting: gs.Count(g => candidates.Any(c => c.GroupId == g.Id)),
                GroupsAgreedHome: gs.Count(Agreed));
        }

        var weeks = new List<FunnelWeek>();
        var monday = now.UtcDateTime.Date.AddDays(-(((int)now.UtcDateTime.DayOfWeek + 6) % 7));
        for (var i = Weeks - 1; i >= 0; i--)
        {
            var from = new DateTimeOffset(monday.AddDays(-7 * i), TimeSpan.Zero); var to = from.AddDays(7);
            bool In(DateTimeOffset? t) => t is { } x && x >= from && x < to;
            weeks.Add(new FunnelWeek(
                from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                people.Values.Count(p => In(p.Account.CreatedAt)),
                people.Values.Count(p => In(p.Account.OnboardedAt)),
                matches.Count(m => In(m.At))));
        }
        return new FunnelView(now, Count(_ => true), Count(p => p.Madrid), weeks);
    }

    static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
