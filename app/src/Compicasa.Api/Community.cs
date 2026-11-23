using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// Everyone the viewer could meet, and the consent rules between them. A like from the viewer is always
/// explicit. Made-up people reply by simulation (they accept if no dealbreaker applies and the score meets
/// their pickiness); real people reply with their own likes.
/// </summary>
public sealed class Community
{
    public required Account Viewer { get; init; }
    public required Profile Me { get; init; }
    public required IReadOnlyDictionary<string, (Account Account, Profile Profile)> Others { get; init; }
    public required IReadOnlyDictionary<string, string> MyReactions { get; init; }
    public required ISet<string> LikedMe { get; init; }

    public static async Task<Community> LoadAsync(AppDb db, Account viewer, CancellationToken ct)
    {
        // Real people who haven't finished onboarding stay out of sight, and so does anyone either side has blocked (B-036).
        var blocked = (await db.Blocks.AsNoTracking().Where(b => b.FromId == viewer.Id || b.ToId == viewer.Id)
            .Select(b => new { b.FromId, b.ToId }).ToListAsync(ct)).Select(b => b.FromId == viewer.Id ? b.ToId : b.FromId).ToHashSet();
        var accounts = (await db.Accounts.AsNoTracking().Where(a => a.Id != viewer.Id && (a.Kind != AccountKind.User || a.OnboardedAt != null)).ToListAsync(ct))
            .Where(a => !blocked.Contains(a.Id)).ToList();
        var mine = await db.Reactions.AsNoTracking().Where(r => r.FromId == viewer.Id).ToDictionaryAsync(r => r.ToId, r => r.Kind, ct);
        var likedMe = await db.Reactions.AsNoTracking().Where(r => r.ToId == viewer.Id && r.Kind == ReactionKind.Like).Select(r => r.FromId).ToListAsync(ct);
        return new Community
        {
            Viewer = viewer,
            Me = viewer.ReadProfile(),
            Others = accounts.ToDictionary(a => a.Id, a => (a, a.ReadProfile())),
            MyReactions = mine,
            LikedMe = likedMe.ToHashSet(),
        };
    }

    public bool ILike(string id) => MyReactions.GetValueOrDefault(id) == ReactionKind.Like;
    public bool IPassed(string id) => MyReactions.GetValueOrDefault(id) == ReactionKind.Pass;

    /// <summary>Has this person said yes to the viewer? Simulated for made-up people.</summary>
    public bool TheyLikeMe(string id) =>
        Others.TryGetValue(id, out var o) && (o.Account.IsDemo ? Matching.Accepts(o.Profile, Me) : LikedMe.Contains(id));

    public bool Mutual(string id) => ILike(id) && TheyLikeMe(id);

    /// <summary>
    /// Has this person liked the viewer before the viewer answered them (B-036, "liked you")? Real people: their own
    /// like. Made-up people: a fixed sixth of those whose simulated rule would say yes, so the list isn't empty in
    /// the demo and stays consistent with their reply when the viewer likes them back.
    /// </summary>
    public bool LikedMeFirst(string id) =>
        Others.TryGetValue(id, out var o) && (o.Account.IsDemo ? Admires(o.Profile.Id) && Matching.Accepts(o.Profile, Me) : LikedMe.Contains(id));

    bool Admires(string id)
    {
        uint h = 2166136261;
        foreach (var ch in Viewer.Id + "|" + id) { h ^= ch; h *= 16777619; }
        return h % 6 == 0;
    }

    public Profile? Get(string id) => id == Viewer.Id ? Me : Others.TryGetValue(id, out var o) ? o.Profile : null;
    public bool IsDemo(string id) => id != Viewer.Id && Others.TryGetValue(id, out var o) && o.Account.IsDemo;

    /// <summary>
    /// Estimated consent between two other people when suggesting or forming a group. Made-up people use
    /// their simulated rule; for real people it is only an estimate, because they answer the invitation themselves.
    /// </summary>
    public bool WouldAccept(Profile a, Profile b) => Matching.Accepts(a, b);

    public IReadOnlyList<Profile> MutualMatches(IEnumerable<string>? exclude = null)
    {
        var ex = exclude?.ToHashSet() ?? [];
        return Others.Values
            .Where(o => !ex.Contains(o.Profile.Id) && Matching.Hard(Me, o.Profile).Ok && Mutual(o.Profile.Id))
            .OrderBy(o => o.Account.Kind == AccountKind.Seed ? 0 : o.Account.Kind == AccountKind.User ? 1 : 2)
            .ThenBy(o => o.Profile.Id, StringComparer.Ordinal)
            .Select(o => o.Profile).ToList();
    }

    /// <summary>Can the viewer form this group? The prototype's groupStatus, with real people's consent left to them.</summary>
    public CheckResult GroupStatus(IReadOnlyList<string> ids)
    {
        var issues = new List<Msg>();
        if (ids.Count == 0) return new CheckResult(false, [new Msg("pickOne")]);
        if (ids.Count > 3) issues.Add(new Msg("tooMany", 3));
        if (ids.Distinct().Count() != ids.Count) issues.Add(new Msg("twice"));
        var others = new List<Profile>();
        foreach (var id in ids.Distinct())
        {
            var p = id == Viewer.Id ? null : Get(id);
            if (p is null) { issues.Add(new Msg("unknownPerson")); continue; }
            others.Add(p);
            if (!Mutual(id)) issues.Add(new Msg("notMutual", id));
        }
        var demos = others.Where(p => IsDemo(p.Id)).ToList();
        if (Matching.OthersReject(demos, WouldAccept) is { } r) issues.Add(new Msg("othersReject", new[] { r.A.Id, r.B.Id }));
        if (others.Count > 0) issues.AddRange(Matching.GroupCheck([Me, .. others]).Issues);
        return new CheckResult(issues.Count == 0, issues);
    }
}
