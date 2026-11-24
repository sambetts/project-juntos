using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>A message sent through the contact form (B-078): the contact point a public platform needs (DSA art. 12,
/// B-072). Read by the admins on <c>#inbox</c>; nothing is emailed until Compicasa has its own address and domain.
/// Feedback from signed-in people (B-081, D-052) is stored here too, with a feedback kind as its topic.</summary>
public sealed class ContactMessage
{
    public static readonly string[] Topics = ["question", "problem", "report", "privacy", "other"];
    /// <summary>Feedback kinds (B-081): an idea, something's wrong, something I like.</summary>
    public static readonly string[] FeedbackKinds = ["idea", "wrong", "like"];
    public const int NameMax = 80, BodyMax = 3000, BodyMin = 10;
    /// <summary>Messages marked as dealt with are deleted this many days later (an Assumption until B-052 sets retention).</summary>
    public const int KeepDoneDays = 365;
    public long Id { get; set; }
    public DateTimeOffset At { get; set; }
    /// <summary>Set when the sender was signed in, so a reply can find their account.</summary>
    public string? AccountId { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; } = "";
    public string Topic { get; set; } = "";
    public string Body { get; set; } = "";
    public string Lang { get; set; } = "es";
    public DateTimeOffset? DoneAt { get; set; }
    /// <summary>Feedback only: "how's Compicasa so far", 1 to 5, if given.</summary>
    public int? Rating { get; set; }
    /// <summary>Feedback only: the kind of screen it was sent from (<see cref="Feedback.PageShape"/>), never an id.</summary>
    public string? Page { get; set; }
}

/// <summary><c>Website</c> is a field people never see; a bot that fills it in is thanked and ignored.</summary>
public sealed record ContactRequest(string? Name, string? Email, string? Topic, string? Body, string? Lang, string? Website);

public sealed record FeedbackRequest(string? Kind, string? Body, int? Rating, string? Page, string? Lang);

/// <summary>In-app feedback (B-081, D-052): signed-in people only, stored with their account and read on <c>#inbox</c>.</summary>
public static class Feedback
{
    public const int BodyMin = 3;

    /// <summary>The screen a piece of feedback came from, as its kind only: ids of people, chats and groups are dropped
    /// so the note never says whose page it was. Anything we don't recognise is not kept.</summary>
    public static string? PageShape(string? hash)
    {
        var h = (hash ?? "").TrimStart('#');
        if (h.Length is 0 or > 120) return null;
        if (Regex.IsMatch(h, "^(home|profile|groups|houses|alerts|messages|preview|preview-brief|people(-(review|liked|matches|passed))?)$")) return h;
        if (Regex.IsMatch(h, "^person-[A-Za-z0-9_.-]{1,64}$")) return "person";
        if (Regex.IsMatch(h, "^chat-[A-Za-z0-9_.-]{1,64}$")) return "chat";
        var g = Regex.Match(h, "^group-[0-9a-f-]{36}(?:-(brief|homes|shortlist|chat))?$");
        if (g.Success) return g.Groups[1].Success ? "group-" + g.Groups[1].Value : "group";
        return null;
    }

    static IResult Error(string code) => Results.Json(new { error = code }, statusCode: 400);

    public static void Map(WebApplication app)
    {
        app.MapPost("/api/feedback", async (FeedbackRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var id = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (id is null) return Results.Unauthorized();
            if (r.Kind is null || !ContactMessage.FeedbackKinds.Contains(r.Kind)) return Error("badKind");
            if (r.Rating is { } n && n is < 1 or > 5) return Error("badRating");
            var body = (r.Body ?? "").Trim();
            if (body.Length is < BodyMin or > ContactMessage.BodyMax) return Error("badFeedbackBody");
            // Minimum data: the account links it to the person, so their email isn't copied here.
            db.ContactMessages.Add(new ContactMessage
            {
                At = clock.GetUtcNow(), AccountId = id, Topic = r.Kind, Body = body, Rating = r.Rating,
                Page = PageShape(r.Page), Lang = r.Lang == "en" ? "en" : "es",
            });
            await db.SaveChangesAsync(http.RequestAborted);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("feedback");
    }
}

public static class Contact
{
    static IResult Error(string code) => Results.Json(new { error = code }, statusCode: 400);

    public static void Map(WebApplication app)
    {
        var cfg = app.Configuration;

        // Open to anyone, signed in or not: the contact point has to be reachable without an account.
        app.MapPost("/api/contact", async (ContactRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (!string.IsNullOrEmpty(r.Website)) return Results.NoContent();
            var email = Accounts.NormaliseEmail(r.Email);
            if (email is null) return Error("badEmail");
            if (r.Topic is null || !ContactMessage.Topics.Contains(r.Topic)) return Error("badTopic");
            var name = (r.Name ?? "").Trim();
            if (name.Length > ContactMessage.NameMax) return Error("badName");
            var body = (r.Body ?? "").Trim();
            if (body.Length is < ContactMessage.BodyMin or > ContactMessage.BodyMax) return Error("badContactBody");
            var id = http.User.Identity?.IsAuthenticated == true ? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            db.ContactMessages.Add(new ContactMessage
            {
                At = clock.GetUtcNow(), AccountId = id, Name = name.Length > 0 ? name : null, Email = email,
                Topic = r.Topic, Body = body, Lang = r.Lang == "en" ? "en" : "es",
            });
            await db.SaveChangesAsync(http.RequestAborted);
            return Results.NoContent();
        }).RequireRateLimiting("contact");

        app.MapGet("/api/admin/contact", async (HttpContext http, AppDb db, TimeProvider clock, CancellationToken ct) =>
        {
            if (!await Funnel.AllowedAsync(http, db, cfg, ct)) return Results.Ok(new { allowed = false });
            var cutoff = clock.GetUtcNow().AddDays(-ContactMessage.KeepDoneDays);
            // SQLite can't compare DateTimeOffset values, so the old ones are found in memory (a small table).
            var old = (await db.ContactMessages.Where(m => m.DoneAt != null).Select(m => new { m.Id, m.DoneAt }).ToListAsync(ct))
                .Where(m => m.DoneAt < cutoff).Select(m => m.Id).ToList();
            if (old.Count > 0) await db.ContactMessages.Where(m => old.Contains(m.Id)).ExecuteDeleteAsync(ct);
            var all = await db.ContactMessages.ToListAsync(ct);
            var page = all.OrderBy(m => m.DoneAt is not null).ThenByDescending(m => m.At).Take(200).ToList();
            // Feedback doesn't copy the sender's email; look it up from the account so an admin can reply.
            var ids = page.Where(m => m.AccountId != null && m.Email.Length == 0).Select(m => m.AccountId!).Distinct().ToList();
            var emails = ids.Count == 0 ? new Dictionary<string, string>() : await db.Accounts.Where(a => ids.Contains(a.Id) && a.Email != null)
                .ToDictionaryAsync(a => a.Id, a => a.Email!, ct);
            var list = page.Select(m => new
            {
                m.Id, m.At, m.AccountId, m.Name, m.Topic, m.Body, m.Lang, m.DoneAt, m.Rating, m.Page,
                Email = m.Email.Length > 0 ? m.Email : (m.AccountId != null && emails.TryGetValue(m.AccountId, out var e) ? e : ""),
                Feedback = ContactMessage.FeedbackKinds.Contains(m.Topic),
            });
            // Counts over everything kept, so the filters show how much of each kind there is.
            var counts = all.GroupBy(m => m.Topic).ToDictionary(g => g.Key, g => new { all = g.Count(), open = g.Count(m => m.DoneAt is null) });
            var rated = all.Where(m => m.Rating is not null).Select(m => m.Rating!.Value).ToList();
            return Results.Ok(new { allowed = true, messages = list, counts,
                ratings = new { count = rated.Count, average = rated.Count == 0 ? (double?)null : Math.Round(rated.Average(), 1) } });
        }).RequireAuthorization();

        app.MapPost("/api/admin/contact/{id:long}/done", async (long id, HttpContext http, AppDb db, TimeProvider clock, CancellationToken ct) =>
        {
            if (!await Funnel.AllowedAsync(http, db, cfg, ct)) return Results.NotFound();
            var m = await db.ContactMessages.FindAsync([id], ct);
            if (m is null) return Results.NotFound();
            m.DoneAt ??= clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
