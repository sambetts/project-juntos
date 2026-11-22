using Compicasa.Core;

namespace Compicasa.Api;

/// <summary>Shapes API responses. Other people are shown through <see cref="PublicProfile"/> only.</summary>
public static class Views
{
    public sealed record Person(string Id, string Name, string Color, string Household, int Adults, int Kids, bool IsDemo, bool IsMe);

    static Person PersonOf(Community c, Profile p) => new(p.Id, p.Name, p.Color, p.Household, p.Adults, p.Kids, c.IsDemo(p.Id), p.Id == c.Viewer.Id);

    static Dictionary<string, Person> People(Community c, IEnumerable<Profile> ps) =>
        ps.DistinctBy(p => p.Id).ToDictionary(p => p.Id, p => PersonOf(c, p));

    static string State(Community c, string id) =>
        c.IPassed(id) ? "passed" : !c.ILike(id) ? "none" : c.TheyLikeMe(id) ? "mutual" : c.IsDemo(id) ? "noMatch" : "waiting";

    public static object Matches(Community c)
    {
        var ok = new List<(Profile P, Explanation Ex)>();
        var hidden = new List<(Profile P, HardResult H)>();
        foreach (var (acc, p) in c.Others.Values)
        {
            var h = Matching.Hard(c.Me, p);
            if (h.Ok) ok.Add((p, Matching.Explain(c.Me, p))); else hidden.Add((p, h));
        }
        ok = ok.OrderByDescending(x => x.Ex.Score.Total).ThenBy(x => x.P.Id, StringComparer.Ordinal).ToList();
        var passed = c.MyReactions.Where(r => r.Value == ReactionKind.Pass).Select(r => r.Key).ToList();
        var mutuals = c.MutualMatches(passed);
        var suggestions = Matching.BestGroups(c.Me, mutuals, 3, c.WouldAccept);
        return new
        {
            me = c.Viewer.Id,
            compatible = ok.Select(x =>
            {
                var hb = Matching.Brief([c.Me, x.P]);
                return new
                {
                    person = PublicProfile.From(x.P, c.IsDemo(x.P.Id)),
                    score = new { total = x.Ex.Score.Total, parts = x.Ex.Score.Parts },
                    clicks = x.Ex.Clicks,
                    talks = x.Ex.Talks,
                    together = new { beds = hb.Beds, baths = hb.Baths, cars = hb.Cars, types = hb.Types },
                    state = State(c, x.P.Id),
                };
            }),
            hidden = hidden.Select(x => new { person = PersonOf(c, x.P), reasons = x.H.Reasons }),
            mutualCount = mutuals.Count,
            likedCount = c.MyReactions.Count(r => r.Value == ReactionKind.Like),
            suggestions = suggestions.Select(g =>
            {
                var all = new List<Profile> { c.Me };
                all.AddRange(g.Ids.Select(id => c.Get(id)!));
                var hb = Matching.Brief(all);
                return new { ids = g.Ids, min = g.Min, avg = g.Avg, size = g.Size, beds = hb.Beds, types = hb.Types, areas = hb.Areas };
            }),
            people = People(c, c.Others.Values.Select(o => o.Profile).Append(c.Me)),
        };
    }

    public static IReadOnlyList<string> OthersInOrder(Group g, string viewerId) =>
        g.Members.Where(m => m.AccountId != viewerId && m.Status != MemberStatus.Left).OrderBy(m => m.Position).Select(m => m.AccountId).ToList();

    static string GroupState(Group g)
    {
        var active = g.Members.Where(m => m.Status != MemberStatus.Left).ToList();
        if (active.Count < 2 || active.Any(m => m.Status == MemberStatus.Declined) || g.Members.Any(m => m.Status == MemberStatus.Left)) return "broken";
        return active.All(m => m.Status == MemberStatus.Accepted) ? "formed" : "pending";
    }

    public static object GroupSummary(Community c, Group g)
    {
        var members = g.Members.OrderBy(m => m.Position).Where(m => c.Get(m.AccountId) is not null).ToList();
        return new
        {
            id = g.Id,
            state = GroupState(g),
            createdBy = g.CreatedById,
            myStatus = g.Members.First(m => m.AccountId == c.Viewer.Id).Status,
            members = members.Select(m => new { id = m.AccountId, status = m.Status }),
            people = People(c, members.Select(m => c.Get(m.AccountId)!)),
        };
    }

    /// <summary>The home brief, from the viewer's side. With <paramref name="g"/> null it is a preview.</summary>
    public static object Home(Community c, IReadOnlyList<string> otherIds, Group? g)
    {
        var all = new List<Profile> { c.Me };
        all.AddRange(otherIds.Select(c.Get).OfType<Profile>());
        var check = Matching.GroupCheck(all);
        var gs = Matching.GroupScore(all);
        var weakest = gs.Weakest is { } w ? Matching.Score(c.Get(w.A)!, c.Get(w.B)!).Parts : null;
        var hb = Matching.Brief(all);
        return new
        {
            id = g?.Id,
            preview = g is null,
            state = g is null ? "preview" : GroupState(g),
            createdBy = g?.CreatedById,
            members = g is null
                ? all.Select(p => new { id = p.Id, status = "preview" }).ToList()
                : g.Members.OrderBy(m => m.Position).Where(m => c.Get(m.AccountId) is not null).Select(m => new { id = m.AccountId, status = m.Status }).ToList(),
            people = People(c, all.Concat(g?.Members.Select(m => c.Get(m.AccountId)).OfType<Profile>() ?? [])),
            check,
            score = new { gs.Min, gs.Avg, gs.Pairs, gs.Weakest, weakestParts = weakest },
            brief = hb,
            size = new { min = g?.M2Min ?? hb.M2Min, max = g?.M2Max ?? hb.M2Max },
            rooms = Matching.RoomPlan(all),
            cars = all.ToDictionary(p => p.Id, p => p.Cars),
            tips = Matching.PlacementTips(all, c.Viewer.Id),
            topics = Matching.AgreementTopics(all),
        };
    }
}
