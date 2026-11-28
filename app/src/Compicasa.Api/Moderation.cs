using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

public sealed record CloseReportRequest(string? Outcome);
public sealed record SuspendRequest(string? Ground, string? Facts);

/// <summary>
/// The back office for reports and suspensions (B-041, D-062). A person, one of the admins listed in
/// <c>Admin:AccountIds</c>, reads every report and decides; nothing is automated. Suspending someone records a statement
/// of reasons they can read when they next open the app (DSA art. 17): what was restricted, the facts, the rule in the
/// terms, that no automated means were used, and how to appeal (the contact form). Whoever reported is told the report
/// was reviewed and whether action was taken (DSA art. 16(5)), never what or about whom beyond that.
/// </summary>
public static class Moderation
{
    /// <summary>The rules in the terms an account can be suspended under; the wording is in i18n.js (<c>grounds</c>).</summary>
    public static readonly string[] Grounds = ["fake", "harassment", "scam", "unsafe", "spam", "illegal", "other"];
    public static readonly string[] Outcomes = ["noBreach", "suspended"];
    public const int FactsMin = 20, FactsMax = 1000;
    /// <summary>Reports about an account that has since been deleted are kept this long for safety, then deleted (B-052;
    /// the privacy notice says "up to two years"; an Assumption until a lawyer reviews it).</summary>
    public const int KeepReportsDays = 730;

    /// <summary>Deletes reports about deleted accounts once they are older than <see cref="KeepReportsDays"/>. Returns how many.</summary>
    public static async Task<int> PurgeAsync(AppDb db, DateTimeOffset now, CancellationToken ct)
    {
        var cutoff = now.AddDays(-KeepReportsDays);
        // SQLite can't compare DateTimeOffset values, so the dates are checked in memory (few rows).
        var orphans = (await db.Reports.Where(r => !db.Accounts.Any(a => a.Id == r.AboutId)).Select(r => new { r.Id, r.At }).ToListAsync(ct))
            .Where(r => r.At < cutoff).Select(r => r.Id).ToList();
        return orphans.Count == 0 ? 0 : await db.Reports.Where(r => orphans.Contains(r.Id)).ExecuteDeleteAsync(ct);
    }

    static IResult Error(string code, int status = 400) => Results.Json(new { error = code }, statusCode: status);

    /// <summary>What a suspended account can still reach: who it is and its statement of reasons, its data, deleting it,
    /// signing out and the contact form (to appeal).</summary>
    static bool OpenWhileSuspended(HttpRequest r)
    {
        var p = r.Path.Value ?? "";
        return p is "/api/config" or "/api/contact" or "/api/homes/sources" or "/api/me/export"
            || p.StartsWith("/api/auth/", StringComparison.Ordinal)
            || (p == "/api/me" && (HttpMethods.IsGet(r.Method) || HttpMethods.IsDelete(r.Method)));
    }

    /// <summary>Stops a suspended account at the door: one place, so no new endpoint can forget the check.</summary>
    public static void UseSuspensions(WebApplication app) => app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.User.Identity?.IsAuthenticated == true && !OpenWhileSuspended(ctx.Request)
            && ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } id)
        {
            var db = ctx.RequestServices.GetRequiredService<AppDb>();
            if (await db.Accounts.AsNoTracking().AnyAsync(a => a.Id == id && a.SuspendedAt != null, ctx.RequestAborted))
            {
                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(new { error = "suspended" });
                return;
            }
        }
        await next();
    });

    /// <summary>The statement of reasons as the person sees it (null if not suspended).</summary>
    public static object? Statement(Account a) => a.SuspendedAt is null ? null : new { at = a.SuspendedAt, ground = a.SuspendGround, facts = a.SuspendFacts };

    /// <summary>Suspends an account: hidden from everyone, out of every group it was in, its open reports closed.</summary>
    public static async Task SuspendAsync(AppDb db, Account a, string ground, string facts, string adminId, DateTimeOffset now, CancellationToken ct)
    {
        a.SuspendedAt = now;
        a.SuspendGround = ground;
        a.SuspendFacts = facts;
        var groups = await db.Groups.Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.AccountId == a.Id && (m.Status == MemberStatus.Pending || m.Status == MemberStatus.Accepted))).ToListAsync(ct);
        foreach (var g in groups)
        {
            var mine = g.Members.First(m => m.AccountId == a.Id);
            mine.Status = mine.Status == MemberStatus.Pending ? MemberStatus.Declined : MemberStatus.Left;
            mine.RespondedAt = now;
            // As with a block: the others see a freed place, never why.
            await Lifecycle.NotifyMembersAsync(db, g, mine.Status == MemberStatus.Left ? NoteKind.Left : NoteKind.Declined, a.Id, now, a.Id, ct);
        }
        // Notices about them would point at someone nobody can see any more.
        await db.Notifications.Where(n => n.ActorId == a.Id && n.ReadAt == null).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
        await CloseOpenReportsAsync(db, a.Id, "suspended", adminId, now, ct);
    }

    static async Task CloseOpenReportsAsync(AppDb db, string aboutId, string outcome, string adminId, DateTimeOffset now, CancellationToken ct, long? only = null)
    {
        var open = await db.Reports.Where(r => r.AboutId == aboutId && r.DoneAt == null && (only == null || r.Id == only)).ToListAsync(ct);
        foreach (var r in open)
        {
            r.DoneAt = now; r.Outcome = outcome; r.ReviewedById = adminId;
            await Notes.AddAsync(db, r.FromId, outcome == "suspended" ? NoteKind.ReportActed : NoteKind.ReportNoBreach, null, null, now, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    public static void Map(WebApplication app)
    {
        var cfg = app.Configuration;
        var admin = app.MapGroup("/api/admin").RequireAuthorization();

        admin.MapGet("/reports", async (HttpContext http, AppDb db, SignInEraser eraser, CancellationToken ct) =>
        {
            if (!await Funnel.AllowedAsync(http, db, cfg, ct)) return Results.Ok(new { allowed = false });
            var reports = await db.Reports.AsNoTracking().OrderBy(r => r.DoneAt != null).ThenByDescending(r => r.Id).Take(200).ToListAsync(ct);
            var suspended = await db.Accounts.AsNoTracking().Where(a => a.SuspendedAt != null).ToListAsync(ct);
            var ids = reports.SelectMany(r => new[] { r.FromId, r.AboutId }).Concat(suspended.Select(a => a.Id)).Distinct().ToList();
            var people = await db.Accounts.AsNoTracking().Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);
            var counts = await db.Reports.AsNoTracking().Where(r => ids.Contains(r.AboutId)).GroupBy(r => r.AboutId)
                .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
            object Person(string id) => people.TryGetValue(id, out var a)
                ? new { id, name = a.ReadProfile().Name, email = a.Email, real = !a.IsDemo, createdAt = a.CreatedAt, suspended = a.SuspendedAt is not null, reports = counts.GetValueOrDefault(id) }
                : new { id, name = (string?)null, email = (string?)null, real = false, createdAt = (DateTimeOffset?)null, suspended = false, reports = counts.GetValueOrDefault(id) } as object;
            // The reported conversation, for context: only open reports, only the last 20 messages.
            var threads = reports.Where(r => r.DoneAt == null && r.Thread != null).Select(r => r.Thread!).Distinct().ToList();
            var msgs = threads.Count == 0 ? [] : await db.Messages.AsNoTracking().Where(m => threads.Contains(m.Thread)).OrderByDescending(m => m.Id).Take(400).ToListAsync(ct);
            return Results.Ok(new
            {
                allowed = true,
                reports = reports.Select(r => new
                {
                    r.Id, r.At, r.Reason, r.Note, r.Thread, r.DoneAt, r.Outcome,
                    from = Person(r.FromId), about = Person(r.AboutId),
                    messages = r.Thread is null || r.DoneAt != null ? [] : msgs.Where(m => m.Thread == r.Thread).Take(20).Reverse()
                        .Select(m => new { m.At, from = m.FromId == r.AboutId ? "about" : m.FromId == r.FromId ? "from" : "other", body = m.Script is null ? m.Body : "(" + m.Script + ")" }).ToList(),
                }),
                suspended = suspended.Select(a => new { id = a.Id, name = a.ReadProfile().Name, email = a.Email, at = a.SuspendedAt, ground = a.SuspendGround, facts = a.SuspendFacts }),
                erasures = new { waiting = await db.SignInErasures.CountAsync(ct), configured = eraser.Configured },
            });
        });

        admin.MapPost("/reports/{id:long}/close", async (long id, CloseReportRequest r, HttpContext http, AppDb db, TimeProvider clock, CancellationToken ct) =>
        {
            if (!await Funnel.AllowedAsync(http, db, cfg, ct)) return Results.NotFound();
            if (r.Outcome != "noBreach") return Error("badOutcome");
            var rep = await db.Reports.FindAsync([id], ct);
            if (rep is null) return Results.NotFound();
            if (rep.DoneAt is not null) return Error("alreadyAnswered", 409);
            await CloseOpenReportsAsync(db, rep.AboutId, "noBreach", http.User.FindFirstValue(ClaimTypes.NameIdentifier)!, clock.GetUtcNow(), ct, id);
            return Results.NoContent();
        });

        admin.MapPost("/accounts/{id}/suspend", async (string id, SuspendRequest r, HttpContext http, AppDb db, TimeProvider clock, CancellationToken ct) =>
        {
            if (!await Funnel.AllowedAsync(http, db, cfg, ct)) return Results.NotFound();
            var me = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (id == me) return Error("notYourself");
            if (r.Ground is null || !Grounds.Contains(r.Ground)) return Error("badGround");
            var facts = (r.Facts ?? "").Trim();
            if (facts.Length is < FactsMin or > FactsMax) return Error("badFacts");
            var found = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Kind == AccountKind.User, ct);
            if (found is null) return Results.NotFound();
            if (found.SuspendedAt is not null) return Error("alreadySuspended", 409);
            // Read again inside the transaction: each attempt starts with nothing tracked (Transactions.RunAsync).
            await Transactions.RunAsync(db, async () =>
                await SuspendAsync(db, await db.Accounts.FirstAsync(x => x.Id == id, ct), r.Ground, facts, me, clock.GetUtcNow(), ct), ct);
            return Results.NoContent();
        });

        admin.MapPost("/accounts/{id}/restore", async (string id, HttpContext http, AppDb db, CancellationToken ct) =>
        {
            if (!await Funnel.AllowedAsync(http, db, cfg, ct)) return Results.NotFound();
            var a = await db.Accounts.FirstOrDefaultAsync(x => x.Id == id && x.Kind == AccountKind.User && x.SuspendedAt != null, ct);
            if (a is null) return Results.NotFound();
            a.SuspendedAt = null; a.SuspendGround = null; a.SuspendFacts = null;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
