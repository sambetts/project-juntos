using System.Security.Claims;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>A home on a group's shortlist (B-056): one from our index (D-035), added by a member.</summary>
public sealed class Candidate
{
    public long Id { get; set; }
    public Guid GroupId { get; set; }
    public string HomeId { get; set; } = "";
    public string AddedById { get; set; } = "";
    public DateTimeOffset AddedAt { get; set; }
    /// <summary>The asking price when it was added, to show a change.</summary>
    public int? PriceAtAdd { get; set; }
    public List<CandidateVote> Votes { get; set; } = [];
}

/// <summary>One member's vote on one stage of a candidate. It counts only while the home's price is the one voted on.</summary>
public sealed class CandidateVote
{
    public long CandidateId { get; set; }
    public string AccountId { get; set; } = "";
    /// <summary>0 = view it, 1 = make an offer, 2 = go ahead (<see cref="Shortlist.Stages"/>).</summary>
    public int Stage { get; set; }
    public string Choice { get; set; } = "";
    public string? Reason { get; set; }
    public int? Price { get; set; }
    public DateTimeOffset At { get; set; }
}

public sealed record AddCandidateRequest(string? HomeId);
public sealed record VoteRequest(string? Choice, string? Reason);

/// <summary>
/// House voting (B-056). A group's members add homes from the index to a shortlist and vote on each in three stages:
/// view it, make an offer, go ahead. A stage is agreed when everyone still in the group has said yes to it at the
/// current price; then the next one opens. Nobody sees the others' choices on a stage until everyone has voted on it.
/// A price change, or someone new joining, means the votes no longer add up and the group votes again. Everything is
/// worked out from the votes when asked, so nothing can drift out of step.
/// </summary>
public static class Shortlist
{
    public static readonly string[] Stages = ["view", "offer", "buy"];
    public const int Agreed = 3;
    public static readonly string[] Choices = ["yes", "maybe", "no"];
    public static readonly string[] Reasons = ["price", "area", "size", "condition", "feeling", "other"];
    public const int MaxItems = 20;

    static string? UserId(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier);
    static IResult Error(string code, int status = 400) => Results.Json(new { error = code }, statusCode: status);

    /// <summary>Everyone still in the group, invited or in. All of them must say yes; only those who are in can vote.</summary>
    public static List<string> Voters(Group g) => Lifecycle.Active(g).OrderBy(m => m.Position).Select(m => m.AccountId).ToList();

    static bool Counts(CandidateVote v, int? price) => v.Price == price;

    /// <summary>The first stage not yet agreed, or <see cref="Agreed"/> once all three are. A group of fewer than two
    /// households (broken) can't agree anything.</summary>
    public static int StageOf(Candidate c, int? price, IReadOnlyList<string> voters)
    {
        if (voters.Count < 2) return 0;
        for (var s = 0; s < Stages.Length; s++)
            if (!voters.All(id => c.Votes.Any(v => v.AccountId == id && v.Stage == s && v.Choice == "yes" && Counts(v, price)))) return s;
        return Agreed;
    }

    /// <summary>How a made-up member votes: by how well the home fits the group's brief, the same way every time.</summary>
    public static (string Choice, string? Reason) DemoVote(string memberId, string homeId, int fitScore)
    {
        var h = Stable(memberId + "|" + homeId);
        if (fitScore >= 80) return ("yes", null);
        if (fitScore >= 65) return h % 3 == 0 ? ("maybe", "size") : ("yes", null);
        return h % 2 == 0 ? ("no", h % 4 == 0 ? "price" : "size") : ("maybe", "condition");
    }

    static uint Stable(string s)
    {
        var h = 2166136261u;
        foreach (var ch in s) { h ^= ch; h *= 16777619; }
        return h;
    }

    sealed record Ctx(Group G, Community C, List<string> Voters, Dictionary<string, Home> Homes, HouseBrief Brief, int M2Min, int M2Max, HashSet<string> Stopped, bool DemoHomes, int? Budget);

    static async Task<Ctx> LoadAsync(AppDb db, Community c, Group g, IConfiguration cfg, CancellationToken ct)
    {
        var voters = Voters(g);
        var profiles = voters.Select(c.Get).OfType<Profile>().ToList();
        var hb = Matching.Brief(profiles.Count > 0 ? profiles : [c.Me]);
        var ids = await db.Candidates.Where(x => x.GroupId == g.Id).Select(x => x.HomeId).ToListAsync(ct);
        var homes = await db.Homes.AsNoTracking().Where(h => ids.Contains(h.Id)).ToDictionaryAsync(h => h.Id, ct);
        return new Ctx(g, c, voters, homes, hb, g.M2Min ?? hb.M2Min, g.M2Max ?? hb.M2Max, HomeSources.Stopped(cfg), Compicasa.Api.DemoHomes.Enabled(cfg),
            Budgets.Combined(profiles));
    }

    /// <summary>The home, if the group can still act on it: still advertised, its agency not stopped, made-up homes switched on.</summary>
    static Home? Live(Ctx x, string homeId) =>
        x.Homes.TryGetValue(homeId, out var h) && h.GoneAt is null && !x.Stopped.Contains(h.Source) && (!h.IsDemo || x.DemoHomes) ? h : null;

    static int FitScore(Ctx x, Home h) => HomeFit.Fit(x.Brief, x.M2Min, x.M2Max, h.ToListing(), x.Budget).Score;

    /// <summary>Made-up members vote at once on whatever stage is open for them (after an add, a vote, a price change).</summary>
    static void DemoVotes(Ctx x, List<Candidate> list, DateTimeOffset now)
    {
        var demos = Lifecycle.Active(x.G).Where(m => m.Status == MemberStatus.Accepted && x.C.IsDemo(m.AccountId)).Select(m => m.AccountId).ToList();
        if (demos.Count == 0) return;
        foreach (var cand in list)
        {
            if (Live(x, cand.HomeId) is not { } h) continue;
            // Each stage opens once the one before is agreed, so vote stage by stage until the demos are waiting on someone real.
            for (var guard = 0; guard < Stages.Length; guard++)
            {
                var s = StageOf(cand, h.Price, x.Voters);
                if (s == Agreed) break;
                var added = false;
                foreach (var id in demos.Where(id => !cand.Votes.Any(v => v.AccountId == id && v.Stage == s && Counts(v, h.Price))))
                {
                    // Over the group's combined budget (B-058): a made-up member says no, for the price.
                    var (choice, reason) = x.Budget is int max && h.Price > max ? ("no", (string?)"price") : DemoVote(id, h.Id, FitScore(x, h));
                    var old = cand.Votes.FirstOrDefault(v => v.AccountId == id && v.Stage == s);
                    if (old is null) cand.Votes.Add(old = new CandidateVote { CandidateId = cand.Id, AccountId = id, Stage = s });
                    old.Choice = choice; old.Reason = reason; old.Price = h.Price; old.At = now;
                    added = true;
                }
                if (!added || StageOf(cand, h.Price, x.Voters) == s) break;
            }
        }
    }

    static object View(Ctx x, Candidate cand)
    {
        x.Homes.TryGetValue(cand.HomeId, out var home);
        var live = Live(x, cand.HomeId);
        var price = home?.Price;
        var stage = StageOf(cand, price, x.Voters);
        var me = x.C.Viewer.Id;
        var open = Math.Min(stage, Stages.Length - 1);
        var votes = cand.Votes.Where(v => v.Stage == open && Counts(v, price)).ToDictionary(v => v.AccountId);
        var everyoneVoted = x.Voters.All(votes.ContainsKey);
        // A stage's choices stay hidden until everyone has voted on it (so nobody just follows the first vote).
        var revealed = stage == Agreed || everyoneVoted;
        var fit = home is null ? null : HomeFit.Fit(x.Brief, x.M2Min, x.M2Max, home.ToListing(), x.Budget);
        var stale = cand.Votes.Where(v => !Counts(v, price)).Select(v => v.Price).FirstOrDefault();
        return new
        {
            id = cand.Id,
            home = home is null ? null : new
            {
                home.Id, home.Agency, Url = home.IsDemo ? null : home.Url, home.Price, home.M2, home.Beds, home.Baths, home.Type,
                home.Municipality, home.District, home.Area, Features = home.Features.Split(',', StringSplitOptions.RemoveEmptyEntries),
                home.Energy, home.IsDemo, home.Photo, fit!.Score, fit.Fits, fit.Misses, fit.Check,
            },
            addedBy = cand.AddedById,
            addedAt = cand.AddedAt,
            priceAtAdd = cand.PriceAtAdd,
            // Votes made at an earlier price no longer count; the group is asked again.
            repriced = cand.Votes.Any(v => !Counts(v, price)) ? new { from = stale, to = price } : null,
            gone = live is null,
            stage,
            stageName = stage == Agreed ? "agreed" : Stages[stage],
            revealed,
            votes = x.Voters.Select(id => votes.TryGetValue(id, out var v)
                ? new { id, voted = true, choice = revealed || id == me ? v.Choice : null, reason = revealed || id == me ? v.Reason : null }
                : new { id, voted = false, choice = (string?)null, reason = (string?)null }),
            canRemove = cand.AddedById == me || x.G.CreatedById == me,
        };
    }

    static async Task<IResult> ListAsync(AppDb db, Ctx x, DateTimeOffset now, CancellationToken ct)
    {
        var list = await db.Candidates.Include(c => c.Votes).Where(c => c.GroupId == x.G.Id).OrderBy(c => c.Id).ToListAsync(ct);
        DemoVotes(x, list, now);
        await db.SaveChangesAsync(ct);
        var me = x.G.Members.FirstOrDefault(m => m.AccountId == x.C.Viewer.Id);
        var items = list.Select(c => (Cand: c, V: View(x, c)))
            // Agreed first, then the furthest along, then the newest.
            .OrderByDescending(t => StageOf(t.Cand, x.Homes.GetValueOrDefault(t.Cand.HomeId)?.Price, x.Voters)).ThenByDescending(t => t.Cand.Id)
            .Select(t => t.V).ToList();
        return Results.Ok(new
        {
            stages = Stages, reasons = Reasons, max = MaxItems,
            voters = x.Voters,
            canVote = me?.Status == MemberStatus.Accepted && Lifecycle.State(x.G) != "broken",
            state = Lifecycle.State(x.G),
            items,
        });
    }

    public static void Map(WebApplication app)
    {
        var me = app.MapGroup("/api/groups/{id:guid}/shortlist").RequireAuthorization();

        async Task<(Account? A, Group? G, Community? C, IResult? Stop)> Open(Guid id, HttpContext http, AppDb db, bool mustBeIn)
        {
            var uid = UserId(http.User);
            var a = uid is null ? null : await db.Accounts.FirstOrDefaultAsync(x => x.Id == uid && x.Kind == AccountKind.User && x.OnboardedAt != null, http.RequestAborted);
            if (a is null) return (null, null, null, Results.Unauthorized());
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id, http.RequestAborted);
            var m = g?.Members.FirstOrDefault(x => x.AccountId == a.Id);
            if (g is null || m is null || !MemberStatus.IsActive(m.Status)) return (a, null, null, Error("notFound", 404));
            if (mustBeIn && m.Status != MemberStatus.Accepted) return (a, null, null, Error("notMember", 403));
            return (a, g, await Community.LoadAsync(db, a, http.RequestAborted), null);
        }

        me.MapGet("", async (Guid id, HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock) =>
        {
            var (_, g, c, stop) = await Open(id, http, db, false); if (stop is not null) return stop;
            var x = await LoadAsync(db, c!, g!, cfg, http.RequestAborted);
            return await ListAsync(db, x, clock.GetUtcNow(), http.RequestAborted);
        });

        me.MapPost("", async (Guid id, AddCandidateRequest r, HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock) =>
        {
            var (a, g, c, stop) = await Open(id, http, db, true); if (stop is not null) return stop;
            if (Lifecycle.State(g!) == "broken") return Error("groupBroken", 409);
            var homeId = r.HomeId ?? "";
            var home = await db.Homes.AsNoTracking().FirstOrDefaultAsync(h => h.Id == homeId, http.RequestAborted);
            if (home is null || home.GoneAt is not null || HomeSources.Stopped(cfg).Contains(home.Source) || (home.IsDemo && !Compicasa.Api.DemoHomes.Enabled(cfg)))
                return Error("unknownHome", 404);
            var have = await db.Candidates.Where(x => x.GroupId == g!.Id).Select(x => x.HomeId).ToListAsync(http.RequestAborted);
            if (have.Contains(homeId)) return Error("alreadyShortlisted", 409);
            if (have.Count >= MaxItems) return Error("shortlistFull", 409);
            var now = clock.GetUtcNow();
            db.Candidates.Add(new Candidate { GroupId = g!.Id, HomeId = homeId, AddedById = a!.Id, AddedAt = now, PriceAtAdd = home.Price });
            await Lifecycle.NotifyMembersAsync(db, g, NoteKind.Shortlisted, a.Id, now, a.Id, http.RequestAborted);
            try { await db.SaveChangesAsync(http.RequestAborted); }
            catch (DbUpdateException)
            {
                // Another member added the same home a moment earlier (unique group and home).
                db.ChangeTracker.Clear();
                if (await db.Candidates.AnyAsync(x => x.GroupId == g.Id && x.HomeId == homeId, http.RequestAborted)) return Error("alreadyShortlisted", 409);
                throw;
            }
            var x = await LoadAsync(db, c!, g, cfg, http.RequestAborted);
            return await ListAsync(db, x, now, http.RequestAborted);
        });

        me.MapDelete("/{cid:long}", async (Guid id, long cid, HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock) =>
        {
            var (a, g, c, stop) = await Open(id, http, db, true); if (stop is not null) return stop;
            var cand = await db.Candidates.FirstOrDefaultAsync(x => x.Id == cid && x.GroupId == g!.Id, http.RequestAborted);
            if (cand is null) return Error("notFound", 404);
            // Whoever added it, or the person who proposed the group, may take it off.
            if (cand.AddedById != a!.Id && g!.CreatedById != a.Id) return Error("notYours", 403);
            await db.CandidateVotes.Where(v => v.CandidateId == cid).ExecuteDeleteAsync(http.RequestAborted);
            db.Candidates.Remove(cand);
            await db.SaveChangesAsync(http.RequestAborted);
            var x = await LoadAsync(db, c!, g!, cfg, http.RequestAborted);
            return await ListAsync(db, x, clock.GetUtcNow(), http.RequestAborted);
        });

        me.MapPut("/{cid:long}/vote", async (Guid id, long cid, VoteRequest r, HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock) =>
        {
            var (a, g, c, stop) = await Open(id, http, db, true); if (stop is not null) return stop;
            if (Lifecycle.State(g!) == "broken") return Error("groupBroken", 409);
            if (r.Choice is null || !Choices.Contains(r.Choice)) return Error("badVote");
            if (r.Reason is not null && !Reasons.Contains(r.Reason)) return Error("badReason");
            var cand = await db.Candidates.Include(x => x.Votes).FirstOrDefaultAsync(x => x.Id == cid && x.GroupId == g!.Id, http.RequestAborted);
            if (cand is null) return Error("notFound", 404);
            var x = await LoadAsync(db, c!, g!, cfg, http.RequestAborted);
            if (Live(x, cand.HomeId) is not { } home) return Error("homeGone", 409);
            var before = StageOf(cand, home.Price, x.Voters);
            if (before == Agreed) return Error("alreadyAgreed", 409);
            var now = clock.GetUtcNow();
            var v = cand.Votes.FirstOrDefault(v => v.AccountId == a!.Id && v.Stage == before);
            if (v is null) cand.Votes.Add(v = new CandidateVote { CandidateId = cand.Id, AccountId = a!.Id, Stage = before });
            v.Choice = r.Choice;
            v.Reason = r.Choice == "yes" ? null : r.Reason;
            v.Price = home.Price;
            v.At = now;
            DemoVotes(x, [cand], now);
            var after = StageOf(cand, home.Price, x.Voters);
            // Everyone hears when a stage is agreed, and when the whole group has agreed on a home.
            if (after > before)
                await Lifecycle.NotifyMembersAsync(db, g!, after == Agreed ? NoteKind.Agreed : after == 1 ? NoteKind.AgreeView : NoteKind.AgreeOffer, a!.Id, now, null, http.RequestAborted);
            await db.SaveChangesAsync(http.RequestAborted);
            return await ListAsync(db, x, now, http.RequestAborted);
        });
    }

    /// <summary>Removes the shortlists of these groups and these people's votes (account deleted, daily batch replaced).</summary>
    public static async Task ForgetAsync(AppDb db, IReadOnlyCollection<string> accounts, IReadOnlyCollection<Guid> groups, CancellationToken ct)
    {
        var cands = await db.Candidates.Where(c => groups.Contains(c.GroupId)).Select(c => c.Id).ToListAsync(ct);
        await db.CandidateVotes.Where(v => cands.Contains(v.CandidateId) || accounts.Contains(v.AccountId)).ExecuteDeleteAsync(ct);
        await db.Candidates.Where(c => cands.Contains(c.Id)).ExecuteDeleteAsync(ct);
    }
}
