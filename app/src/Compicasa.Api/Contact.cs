using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>A message sent through the contact form (B-078): the contact point a public platform needs (DSA art. 12,
/// B-072). Read by the admins on <c>#inbox</c>; nothing is emailed until Compicasa has its own address and domain.</summary>
public sealed class ContactMessage
{
    public static readonly string[] Topics = ["question", "problem", "report", "privacy", "other"];
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
}

/// <summary><c>Website</c> is a field people never see; a bot that fills it in is thanked and ignored.</summary>
public sealed record ContactRequest(string? Name, string? Email, string? Topic, string? Body, string? Lang, string? Website);

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
            var list = (await db.ContactMessages.ToListAsync(ct))
                .OrderBy(m => m.DoneAt is not null).ThenByDescending(m => m.At).Take(200)
                .Select(m => new { m.Id, m.At, m.AccountId, m.Name, m.Email, m.Topic, m.Body, m.Lang, m.DoneAt });
            return Results.Ok(new { allowed = true, messages = list });
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
