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

        me.MapGet("/me", async (HttpContext http, AppDb db) =>
            await CurrentAsync(http, db) is { } a ? Results.Ok(Accounts.Me(a)) : Results.Unauthorized());

        me.MapDelete("/me", async (HttpContext http, AppDb db) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            await db.Reactions.Where(x => x.FromId == a.Id || x.ToId == a.Id).ExecuteDeleteAsync();
            var created = await db.Groups.Where(g => g.CreatedById == a.Id).Select(g => g.Id).ToListAsync();
            await db.GroupMembers.Where(m => created.Contains(m.GroupId) || m.AccountId == a.Id).ExecuteDeleteAsync();
            await db.Groups.Where(g => created.Contains(g.Id)).ExecuteDeleteAsync();
            await db.Accounts.Where(x => x.Id == a.Id).ExecuteDeleteAsync();
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
            a.OnboardedAt ??= clock.GetUtcNow();
            await db.SaveChangesAsync();
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

        me.MapGet("/matches", async (HttpContext http, AppDb db) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Matches(c));
        });

        me.MapPut("/reactions/{id}", async (string id, ReactionRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var (a, stop) = await OnboardedAsync(http, db); if (a is null) return stop!;
            if (id == a.Id || !await db.Accounts.AnyAsync(x => x.Id == id && (x.Kind != AccountKind.User || x.OnboardedAt != null))) return Error("unknownPerson", 404);
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
            var g = new Group { Id = Guid.NewGuid(), CreatedById = a.Id, CreatedAt = now };
            g.Members.Add(new GroupMember { AccountId = a.Id, Position = 0, Status = MemberStatus.Accepted, RespondedAt = now });
            var everyone = new List<Profile> { c.Me };
            everyone.AddRange(ids.Select(id => c.Get(id)!));
            for (var i = 0; i < ids.Count; i++)
            {
                var m = new GroupMember { AccountId = ids[i], Position = i + 1, Status = MemberStatus.Pending };
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
            await db.SaveChangesAsync();
            return Results.Ok(Views.Home(c, ids, g));
        });

        me.MapGet("/groups", async (HttpContext http, AppDb db) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var groups = await db.Groups.AsNoTracking().Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.AccountId == a.Id && m.Status != MemberStatus.Left)).ToListAsync();
            return Results.Ok(groups.OrderByDescending(g => g.CreatedAt.UtcTicks).Select(g => Views.GroupSummary(c, g)));
        });

        me.MapGet("/groups/{id:guid}", async (Guid id, HttpContext http, AppDb db) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.AsNoTracking().Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !g.Members.Any(m => m.AccountId == a.Id && m.Status != MemberStatus.Left)) return Error("notFound", 404);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        // Homes for sale that fit the group's brief (D-035). Each carries its agency and the link to its own advert.
        me.MapGet("/groups/{id:guid}/homes", async (Guid id, HttpContext http, AppDb db, IConfiguration cfg) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.AsNoTracking().Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g is null || !g.Members.Any(m => m.AccountId == a.Id && m.Status != MemberStatus.Left)) return Error("notFound", 404);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var all = new List<Profile> { c.Me };
            all.AddRange(Views.OthersInOrder(g, a.Id).Select(c.Get).OfType<Profile>());
            var hb = Matching.Brief(all);
            int lo = g.M2Min ?? hb.M2Min, hi = g.M2Max ?? hb.M2Max;
            var stopped = HomeSources.Stopped(cfg);
            var q = db.Homes.AsNoTracking().Where(h => h.GoneAt == null && h.Beds >= hb.Beds);
            if (hb.Areas.Count > 0) q = q.Where(h => h.Area != null && hb.Areas.Contains(h.Area));
            var candidates = (await q.ToListAsync(http.RequestAborted)).Where(h => !stopped.Contains(h.Source)).Select(h => h.ToListing()).ToList();
            // Up to 200, so the group can filter, sort and chart them in the browser (Session 16 design review).
            var ranked = HomeFit.Rank(hb, lo, hi, candidates, 200);
            return Results.Ok(new
            {
                considered = candidates.Count,
                real = candidates.Count(h => !h.IsDemo),
                homes = ranked.Select(x => new
                {
                    x.Home.Id, x.Home.Agency, x.Home.Url, x.Home.Price, x.Home.M2, x.Home.Beds, x.Home.Baths, x.Home.Type,
                    x.Home.Municipality, x.Home.District, x.Home.Area, x.Home.Features, x.Home.Energy, x.Home.IsDemo,
                    x.Fit.Score, x.Fit.Fits, x.Fit.Misses, x.Fit.Check,
                }),
            });
        });

        me.MapPost("/groups/{id:guid}/respond", async (Guid id, RespondRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            var m = g?.Members.FirstOrDefault(x => x.AccountId == a.Id);
            if (g is null || m is null || m.Status == MemberStatus.Left) return Error("notFound", 404);
            if (m.Status != MemberStatus.Pending) return Error("alreadyAnswered", 409);
            m.Status = r.Accept ? MemberStatus.Accepted : MemberStatus.Declined;
            m.RespondedAt = clock.GetUtcNow();
            await db.SaveChangesAsync();
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            return Results.Ok(Views.Home(c, Views.OthersInOrder(g, a.Id), g));
        });

        me.MapPost("/groups/{id:guid}/leave", async (Guid id, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await CurrentAsync(http, db) is not { } a) return Results.Unauthorized();
            var g = await db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            var m = g?.Members.FirstOrDefault(x => x.AccountId == a.Id);
            if (g is null || m is null) return Error("notFound", 404);
            m.Status = MemberStatus.Left;
            m.RespondedAt = clock.GetUtcNow();
            await db.SaveChangesAsync();
            return Results.NoContent();
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
}
