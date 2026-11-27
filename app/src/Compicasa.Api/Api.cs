using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Compicasa.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

public sealed record ReactionRequest(string? Kind);
public sealed record IdsRequest(List<string>? Ids);
public sealed record RespondRequest(bool Accept);
public sealed record SizeRequest(int Min, int Max);

public static partial class Api
{
    public const string CsrfHeader = "X-Compicasa";
    static IResult Error(string code, int status = 400, IEnumerable<Msg>? issues = null) =>
        Results.Json(new { error = code, issues }, statusCode: status);

    static string? UserId(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier);

    static async Task<Account?> CurrentAsync(HttpContext http, AppDb db)
    {
        var id = UserId(http.User);
        return id is null ? null : await db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.Kind == AccountKind.User, http.RequestAborted);
    }

    /// <summary>The signed-in person, if they've finished onboarding; otherwise the error to send.</summary>
    static async Task<(Account? Account, IResult? Error)> OnboardedAsync(HttpContext http, AppDb db)
    {
        var a = await CurrentAsync(http, db);
        if (a is null) return (null, Results.Unauthorized());
        return a.OnboardedAt is null ? (null, Error("onboardingNeeded", 409)) : (a, null);
    }

    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api");
        var me = api.MapGroup("").RequireAuthorization();

        // Where homes come from (#homes-sources): open to anyone, like the guide and the /bot page. Agencies and counts only.
        api.MapGet("/homes/sources", async (AppDb db, IConfiguration cfg, CancellationToken ct) =>
            Results.Ok(new { sources = await HomeSources.SummariesAsync(db, cfg, ct), portals = HomeSources.Portals }));

        me.MapGet("/me", async (HttpContext http, AppDb db) =>
            await CurrentAsync(http, db) is { } a ? Results.Ok(Accounts.Me(a)) : Results.Unauthorized());

        me.MapDelete("/me", async (HttpContext http, AppDb db) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            // All or nothing: a failure part-way must not leave half an account behind (Session 32 code review).
            await Transactions.RunAsync(db, async () =>
            {
                await db.Reactions.Where(x => x.FromId == a.Id || x.ToId == a.Id).ExecuteDeleteAsync();
                await db.SavedHomes.Where(x => x.AccountId == a.Id).ExecuteDeleteAsync();
                var created = await db.Groups.Where(g => g.CreatedById == a.Id).Select(g => g.Id).ToListAsync();
                await Threads.ForgetAsync(db, [a.Id], created);
                await db.GroupMembers.Where(m => created.Contains(m.GroupId) || m.AccountId == a.Id).ExecuteDeleteAsync();
                await db.Groups.Where(g => created.Contains(g.Id)).ExecuteDeleteAsync();
                await db.Accounts.Where(x => x.Id == a.Id).ExecuteDeleteAsync();
            });
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        // The end of the onboarding wizard: from now on they see matches and others can see them.
        me.MapPost("/me/onboarded", async (HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var p = a.ReadProfile();
            if (p.Name.Trim().Length == 0) return Error("badName");
            if (p.Areas.Count == 0 || p.Types.Count == 0) return Error("profileIncomplete");
            var first = a.OnboardedAt is null;
            a.OnboardedAt ??= clock.GetUtcNow();
            await db.SaveChangesAsync();
            if (first) await NotifyNearbyAsync(db, a, clock.GetUtcNow(), http.RequestAborted);
            return Results.Ok(Accounts.Me(a));
        });

        me.MapGet("/profile", async (HttpContext http, AppDb db) =>
            await CurrentAsync(http, db) is { } a ? Results.Ok(a.ReadProfile()) : Results.Unauthorized());

        me.MapPut("/profile", async (Profile input, HttpContext http, AppDb db) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var (p, problem) = ProfileRules.Clean(input, a.ReadProfile());
            if (problem is not null) return Error(problem);
            a.WriteProfile(p!);
            await db.SaveChangesAsync();
            return Results.Ok(p);
        });

        me.MapGet("/matches", async (HttpContext http, AppDb db, IConfiguration cfg) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Matches(c, EarlyBelow(cfg)));
        });

        me.MapPut("/reactions/{id}", async (string id, ReactionRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            if (id == a.Id || !await db.Accounts.AnyAsync(x => x.Id == id && (x.Kind != AccountKind.User || x.OnboardedAt != null))) return Error("unknownPerson", 404);
            // Someone either side has blocked is out of sight, so they can't be liked or passed (and so can't be notified).
            if (await db.Blocks.AnyAsync(b => (b.FromId == a.Id && b.ToId == id) || (b.FromId == id && b.ToId == a.Id))) return Error("unknownPerson", 404);
            var existing = await db.Reactions.FindAsync(a.Id, id);
            var added = false;
            if (r.Kind is ReactionKind.Like or ReactionKind.Pass)
            {
                if (existing is null) { db.Reactions.Add(existing = new Reaction { FromId = a.Id, ToId = id }); added = true; }
                existing.Kind = r.Kind;
                existing.At = clock.GetUtcNow();
            }
            else if (r.Kind is "none") { if (existing is not null) db.Reactions.Remove(existing); }
            else return Error("badReaction");
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) when (added)
            {
                // Two quick reactions to the same person (B-045): the other request inserted the row first, so update it.
                db.ChangeTracker.Clear();
                var row = await db.Reactions.FindAsync(a.Id, id);
                if (row is null) throw;
                row.Kind = r.Kind;
                row.At = clock.GetUtcNow();
                await db.SaveChangesAsync();
            }
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            if (!c.IsDemo(id)) await NotifyReactionAsync(db, c, id, r.Kind!, clock.GetUtcNow(), http.RequestAborted);
            return Results.Ok(new { mutual = c.Mutual(id), isDemo = c.IsDemo(id) });
        });

        me.MapPost("/preview", async (IdsRequest r, HttpContext http, AppDb db) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var ids = r.Ids ?? [];
            var st = c.GroupStatus(ids);
            if (!st.Ok) return Error("groupInvalid", 422, st.Issues);
            return Results.Ok(Views.Home(c, ids, null));
        });

        me.MapPost("/groups", async (IdsRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var ids = r.Ids ?? [];
            var st = c.GroupStatus(ids);
            if (!st.Ok) return Error("groupInvalid", 422, st.Issues);
            var now = clock.GetUtcNow();
            var g = new Group { Id = Guid.NewGuid(), CreatedById = a.Id, CreatedAt = now, PlannedSize = ids.Count + 1 };
            g.Members.Add(new GroupMember { AccountId = a.Id, Position = 0, Status = MemberStatus.Accepted, RespondedAt = now, InvitedAt = now });
            var everyone = new List<Profile> { c.Me };
            everyone.AddRange(ids.Select(id => c.Get(id)!));
            for (var i = 0; i < ids.Count; i++)
            {
                var m = new GroupMember { AccountId = ids[i], Position = i + 1, Status = MemberStatus.Pending, InvitedAt = now, InvitedById = a.Id };
                if (c.IsDemo(ids[i]))
                {
                    // Made-up people answer at once, by their own rule, about everyone else in the group.
                    var p = c.Get(ids[i])!;
                    m.Status = everyone.Where(o => o.Id != p.Id).All(o => Matching.Accepts(p, o)) ? MemberStatus.Accepted : MemberStatus.Declined;
                    m.RespondedAt = now;
                }
                g.Members.Add(m);
            }
            db.Groups.Add(g);
            foreach (var m in g.Members.Where(m => m.Status == MemberStatus.Pending))
                await Notes.AddAsync(db, m.AccountId, NoteKind.Invite, a.Id, g.Id, now);
            await db.SaveChangesAsync();
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        me.MapGet("/groups", async (HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            await Lifecycle.ExpireAsync(db, a.Id, clock.GetUtcNow(), http.RequestAborted);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var groups = await db.Groups.AsNoTracking().Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.AccountId == a.Id && (m.Status == MemberStatus.Pending || m.Status == MemberStatus.Accepted))).ToListAsync();
            return Results.Ok(groups.OrderByDescending(g => g.CreatedAt.UtcTicks).Select(g => Views.GroupSummary(c, g)));
        });

        me.MapGet("/groups/{id:guid}", async (Guid id, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            await Lifecycle.ExpireAsync(db, a.Id, clock.GetUtcNow(), http.RequestAborted);
            var g = await db.Groups.AsNoTracking().Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !InGroup(g, a.Id)) return Error("notFound", 404);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        // Homes for sale that fit the group's brief (D-035). Each carries its agency and the link to its own advert.
        me.MapGet("/groups/{id:guid}/homes", async (Guid id, HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.AsNoTracking().Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !InGroup(g, a.Id)) return Error("notFound", 404);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var all = new List<Profile> { c.Me };
            all.AddRange(Views.OthersInOrder(g, a.Id).Select(c.Get).OfType<Profile>());
            // Up to 200, so the group can filter, sort and chart them in the browser (Session 16 design review).
            return Results.Ok(await HomesForAsync(db, cfg, clock, all, g.M2Min, g.M2Max, 200, http.RequestAborted));
        });

        // The Home page (Session 20): a few homes for the viewer and their best match, before any group exists.
        // The partner is the best-scoring mutual match, or failing that the best-scoring person not passed on.
        me.MapGet("/me/homes", async (HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var partner = c.Others.Values.Select(o => o.Profile)
                .Where(p => Matching.Hard(c.Me, p).Ok && !c.IPassed(p.Id))
                .Select(p => (P: p, Score: Matching.Explain(c.Me, p).Score.Total))
                .OrderByDescending(x => c.Mutual(x.P.Id)).ThenByDescending(x => x.Score).ThenBy(x => x.P.Id, StringComparer.Ordinal)
                .Select(x => x.P).FirstOrDefault();
            List<Profile> all = partner is null ? [c.Me] : [c.Me, partner];
            var r = await HomesForAsync(db, cfg, clock, all, null, null, 6, http.RequestAborted);
            return Results.Ok(new
            {
                with = partner is null ? null : new { id = partner.Id, name = partner.Name, mutual = c.Mutual(partner.Id), isDemo = c.IsDemo(partner.Id) },
                r.considered, r.real, r.homes, budgetSet = c.Me.MaxBudget is not null, // the pair's total stays hidden (D-044)
            });
        });

        me.MapPost("/groups/{id:guid}/respond", async (Guid id, RespondRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            var m = g?.Members.FirstOrDefault(x => x.AccountId == a.Id);
            if (g is null || m is null || !MemberStatus.IsActive(m.Status))
                return m?.Status is MemberStatus.Expired or MemberStatus.Withdrawn ? Error(m.Status, 409) : Error("notFound", 404);
            if (m.Status != MemberStatus.Pending) return Error("alreadyAnswered", 409);
            var now = clock.GetUtcNow();
            if (Lifecycle.ExpiresAt(g, m) <= now) { await Lifecycle.ExpireAsync(db, a.Id, now); return Error(MemberStatus.Expired, 409); }
            var wasFormed = Lifecycle.State(g) == "formed";
            m.Status = r.Accept ? MemberStatus.Accepted : MemberStatus.Declined;
            m.RespondedAt = now;
            await Lifecycle.NotifyMembersAsync(db, g, r.Accept ? NoteKind.Accepted : NoteKind.Declined, a.Id, now, a.Id);
            await Lifecycle.AfterChangeAsync(db, g, wasFormed, now, a.Id);
            await db.SaveChangesAsync();
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        me.MapPost("/groups/{id:guid}/leave", async (Guid id, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            var m = g?.Members.FirstOrDefault(x => x.AccountId == a.Id);
            if (g is null || m is null || !MemberStatus.IsActive(m.Status)) return Error("notFound", 404);
            var now = clock.GetUtcNow();
            m.Status = m.Status == MemberStatus.Pending ? MemberStatus.Declined : MemberStatus.Left;
            m.RespondedAt = now;
            await Lifecycle.NotifyMembersAsync(db, g, m.Status == MemberStatus.Left ? NoteKind.Left : NoteKind.Declined, a.Id, now, a.Id);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Fill a free place with one of your own mutual matches (B-038).
        me.MapPost("/groups/{id:guid}/invite", async (Guid id, InviteRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var now = clock.GetUtcNow();
            await Lifecycle.ExpireAsync(db, a.Id, now, http.RequestAborted);
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !InGroup(g, a.Id)) return Error("notFound", 404);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var newId = r.Id ?? "";
            if (Lifecycle.CheckInvite(c, g, newId) is { } problem) return Error("cantInvite", 422, [problem]);
            var wasFormed = Lifecycle.State(g) == "formed";
            var row = g.Members.FirstOrDefault(m => m.AccountId == newId);
            if (row is null) g.Members.Add(row = new GroupMember { GroupId = g.Id, AccountId = newId, Position = g.Members.Max(m => m.Position) + 1 });
            row.Status = MemberStatus.Pending; row.InvitedAt = now; row.InvitedById = a.Id; row.RespondedAt = null;
            if (c.IsDemo(newId))
            {
                // A made-up person answers at once, by their own rule, about everyone still in the group.
                var p = c.Get(newId)!;
                var everyone = Lifecycle.Active(g).Where(m => m.AccountId != newId).Select(m => c.Get(m.AccountId)).OfType<Profile>();
                row.Status = everyone.All(o => Matching.Accepts(p, o)) ? MemberStatus.Accepted : MemberStatus.Declined;
                row.RespondedAt = now;
            }
            else await Notes.AddAsync(db, newId, NoteKind.Invite, a.Id, g.Id, now);
            await Lifecycle.NotifyMembersAsync(db, g, NoteKind.Invited, newId, now, a.Id);
            await Lifecycle.AfterChangeAsync(db, g, wasFormed, now, a.Id);
            await db.SaveChangesAsync();
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        // Take back an invitation that hasn't been answered: your own, or any, if you proposed the group (B-038).
        me.MapPost("/groups/{id:guid}/withdraw", async (Guid id, InviteRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !g.Members.Any(m => m.AccountId == a.Id && m.Status == MemberStatus.Accepted)) return Error("notFound", 404);
            var row = g.Members.FirstOrDefault(m => m.AccountId == r.Id && m.Status == MemberStatus.Pending);
            if (row is null) return Error("notPending", 409);
            if ((row.InvitedById ?? g.CreatedById) != a.Id && g.CreatedById != a.Id) return Error("notYours", 403);
            var now = clock.GetUtcNow();
            var wasFormed = Lifecycle.State(g) == "formed";
            row.Status = MemberStatus.Withdrawn;
            row.RespondedAt = now;
            await db.Notifications.Where(n => n.AccountId == row.AccountId && n.GroupId == g.Id && n.Kind == NoteKind.Invite && n.ReadAt == null).ExecuteDeleteAsync();
            await Notes.AddAsync(db, row.AccountId, NoteKind.Withdrawn, a.Id, g.Id, now);
            await Lifecycle.AfterChangeAsync(db, g, wasFormed, now, null);
            await db.SaveChangesAsync();
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        me.MapPut("/groups/{id:guid}/size", async (Guid id, SizeRequest r, HttpContext http, AppDb db) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !g.Members.Any(m => m.AccountId == a.Id && m.Status == MemberStatus.Accepted)) return Error("notFound", 404);
            int lo = Math.Min(r.Min, r.Max), hi = Math.Max(r.Min, r.Max);
            if (lo < 20 || hi > 900) return Error("badSize");
            g.M2Min = lo; g.M2Max = hi;
            await db.SaveChangesAsync();
            return Results.Ok(new { min = lo, max = hi });
        });
    }

    /// <summary>Still in the group: invited and not answered, or said yes. People who declined, left, expired or were
    /// withdrawn no longer see it.</summary>
    static bool InGroup(Group g, string id) => g.Members.Any(m => m.AccountId == id && MemberStatus.IsActive(m.Status));

    /// <summary>Homes for sale that fit these people's shared brief (D-035), best first. Each carries its agency and the link to its own advert.</summary>
    static async Task<HomesResult> HomesForAsync(AppDb db, IConfiguration cfg, TimeProvider clock, IReadOnlyList<Profile> all, int? m2Min, int? m2Max, int limit, CancellationToken ct)
    {
        var hb = Matching.Brief(all);
        int lo = m2Min ?? hb.M2Min, hi = m2Max ?? hb.M2Max;
        var areas = hb.Areas.ToHashSet();
        // From the in-memory index (HomeIndex), not the whole table each time.
        var candidates = (await HomeIndex.LiveAsync(db, cfg, clock, ct))
            .Where(e => e.Home.Beds >= hb.Beds && (areas.Count == 0 || (e.Home.Area is not null && areas.Contains(e.Home.Area))))
            .Select(e => e.Listing).ToList();
        // B-058: the members' maximum budgets added up; homes over it drop out, and we say how many.
        var budget = Budgets.Combined(all);
        var ranked = HomeFit.Rank(hb, lo, hi, candidates, limit, budget);
        var over = budget is null ? 0 : candidates.Count(h => h.Price > budget && HomeFit.Fit(hb, lo, hi, h).Ok);
        var unset = all.Where(p => p.MaxBudget is null).Select(p => p.Id).ToList();
        return new HomesResult(candidates.Count, candidates.Count(h => !h.IsDemo), budget, over, unset, ranked.Select(x => (object)new
        {
            x.Home.Id, x.Home.Agency, x.Home.Url, x.Home.Price, x.Home.M2, x.Home.Beds, x.Home.Baths, x.Home.Type,
            x.Home.Municipality, x.Home.District, x.Home.Area, x.Home.Features, x.Home.Energy, x.Home.IsDemo, Photo = x.Home.PhotoUrl,
            x.Fit.Score, x.Fit.Fits, x.Fit.Misses, x.Fit.Check,
        }).ToList());
    }

    /// <summary>Serialised as-is, so the names are the JSON names.</summary>
    sealed record HomesResult(int considered, int real, int? budget, int overBudget, List<string> budgetUnset, List<object> homes);

    /// <summary>Tells a real person about a like (B-037): "liked you" if they haven't answered, "match" if they had
    /// already liked the viewer. Taking a like back removes the unread notice.</summary>
    /// <summary>Below this many real compatible people left to meet, People and Home show the early-days card (D-059). An Assumption to tune.</summary>
    public static int EarlyBelow(IConfiguration cfg) => Math.Max(0, cfg.GetValue("People:EarlyBelow", 5));

    /// <summary>Most unread "someone compatible joined" notices one person holds at once, so a busy day can't flood them.</summary>
    public const int MaxNearbyUnread = 3;

    /// <summary>
    /// A real person has just finished signing up: tell every real person compatible with them both ways (D-059). These
    /// are exactly the people who will see the newcomer in People, so the notice reveals nothing new. Blocks are respected.
    /// </summary>
    static async Task NotifyNearbyAsync(AppDb db, Account newcomer, DateTimeOffset now, CancellationToken ct)
    {
        var c = await Community.LoadAsync(db, newcomer, ct);
        var them = c.Others.Values.Where(o => !o.Account.IsDemo && Matching.Hard(c.Me, o.Profile).Ok).Select(o => o.Account.Id).ToList();
        if (them.Count == 0) return;
        var full = (await db.Notifications.AsNoTracking().Where(n => them.Contains(n.AccountId) && n.Kind == NoteKind.Nearby && n.ReadAt == null)
            .GroupBy(n => n.AccountId).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct))
            .Where(x => x.N >= MaxNearbyUnread).Select(x => x.Key).ToHashSet();
        foreach (var id in them.Where(id => !full.Contains(id))) await Notes.AddAsync(db, id, NoteKind.Nearby, newcomer.Id, null, now, ct);
        await db.SaveChangesAsync(ct);
    }

    static async Task NotifyReactionAsync(AppDb db, Community c, string to, string kind, DateTimeOffset now, CancellationToken ct)
    {
        await db.Notifications.Where(n => n.AccountId == to && n.ActorId == c.Viewer.Id && n.ReadAt == null
            && (n.Kind == NoteKind.Like || n.Kind == NoteKind.Match)).ExecuteDeleteAsync(ct);
        if (kind != ReactionKind.Like) return;
        var theirs = await db.Reactions.AsNoTracking().FirstOrDefaultAsync(x => x.FromId == to && x.ToId == c.Viewer.Id, ct);
        if (theirs?.Kind == ReactionKind.Pass) return;
        await Notes.AddAsync(db, to, theirs?.Kind == ReactionKind.Like ? NoteKind.Match : NoteKind.Like, c.Viewer.Id, null, now, ct);
        await db.SaveChangesAsync(ct);
    }
}
