using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>Session 39: full account deletion and the data download (B-039); reports reviewed by a person and
/// suspensions with a statement of reasons (B-041, D-062). Real people only, so made-up people don't get in the way.</summary>
public class ModerationTests : IClassFixture<RealOnlyFactory>
{
    readonly RealOnlyFactory _app;
    public ModerationTests(RealOnlyFactory app) => _app = app;

    static int _n;
    static string NewEmail() => $"mod{Interlocked.Increment(ref _n)}.{Guid.NewGuid().ToString("N")[..6]}@example.com";

    async Task<(HttpClient C, string Id, string Email)> PersonAsync(string name)
    {
        var c = _app.Browser();
        var email = NewEmail();
        var id = await AppFactory.SignUpAsync(c, email, name);
        await c.PutAsJsonAsync("/api/profile", ApiTests.RoomProfile(name));
        return (c, id, email);
    }

    async Task<(HttpClient C, string Id)> AdminAsync()
    {
        var (c, id, _) = await PersonAsync("Admin");
        _app.Services.GetRequiredService<IConfiguration>()["Admin:AccountIds"] = id;
        return (c, id);
    }

    T Db<T>(Func<AppDb, T> f)
    {
        using var scope = _app.Services.CreateScope();
        return f(scope.ServiceProvider.GetRequiredService<AppDb>());
    }

    static async Task<string?> ErrorAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
    static async Task<string[]> Kinds(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/inbox")).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("kind").GetString()!).ToArray();

    [Fact]
    public async Task Deleting_needs_a_recent_sign_in()
    {
        var (c, id, email) = await PersonAsync("Dora");
        _app.Clock.Advance(TimeSpan.FromMinutes(11));
        var r = await c.DeleteAsync("/api/me");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("reauthNeeded", await ErrorAsync(r));
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/login", new { email, password = "a-long-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/me")).StatusCode);
        Assert.False(Db(db => db.Accounts.Any(a => a.Id == id)));
    }

    [Fact]
    public async Task Deleting_removes_feedback_and_contact_messages_and_queues_the_sign_in_record()
    {
        var (c, id, _) = await PersonAsync("Elsa");
        var marker = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/feedback", new { kind = "idea", body = "idea " + marker })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/contact", new { topic = "question", email = "elsa@example.com", body = "a question " + marker })).StatusCode);
        var oid = Guid.NewGuid().ToString();
        Db(db => db.Accounts.Where(a => a.Id == id).ExecuteUpdate(u => u.SetProperty(a => a.ExternalId, oid)));
        Assert.Equal(2, Db(db => db.ContactMessages.Count(m => m.Body.Contains(marker))));

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/me")).StatusCode);
        Assert.Equal(0, Db(db => db.ContactMessages.Count(m => m.Body.Contains(marker))));
        // Graph isn't set up in tests, so the request waits for the hourly retry.
        Assert.True(Db(db => db.SignInErasures.Any(x => x.ExternalId == oid)));
    }

    [Fact]
    public async Task Reports_about_a_deleted_account_are_kept_two_years_then_deleted()
    {
        var (a, aId, _) = await PersonAsync("Lia");
        var (b, _, _) = await PersonAsync("Mar");
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync("/api/reports", new { about = aId, reason = "harassment" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync("/api/me")).StatusCode);
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        Assert.Equal(1, await db.Reports.CountAsync(r => r.AboutId == aId));
        Assert.Equal(0, await Moderation.PurgeAsync(db, _app.Clock.GetUtcNow().AddDays(729), default));
        Assert.Equal(1, await Moderation.PurgeAsync(db, _app.Clock.GetUtcNow().AddDays(731), default));
        Assert.Equal(0, await db.Reports.CountAsync(r => r.AboutId == aId));
    }

    [Fact]
    public async Task The_data_download_has_the_account_the_profile_and_activity()
    {
        var (a, aId, email) = await PersonAsync("Flor");
        var (_, bId, _) = await PersonAsync("Gala");
        Assert.Equal(HttpStatusCode.OK, (await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" })).StatusCode);
        var r = await a.GetAsync("/api/me/export");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("attachment", r.Content.Headers.ContentDisposition?.ToString() ?? r.Headers.ToString() + r.Content.Headers.ToString());
        var d = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, d.GetProperty("account").GetProperty("email").GetString());
        Assert.Equal("Flor", d.GetProperty("profile").GetProperty("name").GetString());
        Assert.Equal("Gala", d.GetProperty("likesAndPasses")[0].GetProperty("to").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Browser().GetAsync("/api/me/export")).StatusCode);
    }

    [Fact]
    public async Task Only_admins_see_reports_and_a_report_with_no_breach_tells_the_reporter()
    {
        var (a, aId, _) = await PersonAsync("Hana");
        var (b, bId, _) = await PersonAsync("Ines");
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync("/api/reports", new { about = aId, reason = "fake", note = "seems fake" })).StatusCode);
        Assert.False((await a.GetFromJsonAsync<JsonElement>("/api/admin/reports")).GetProperty("allowed").GetBoolean());

        var (admin, _) = await AdminAsync();
        var list = await admin.GetFromJsonAsync<JsonElement>("/api/admin/reports");
        var rep = list.GetProperty("reports").EnumerateArray().First(x => x.GetProperty("about").GetProperty("id").GetString() == aId);
        Assert.Equal("Ines", rep.GetProperty("from").GetProperty("name").GetString());
        var rid = rep.GetProperty("id").GetInt64();

        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/admin/reports/{rid}/close", new { outcome = "noBreach" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/reports/{rid}/close", new { outcome = "noBreach" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/admin/reports/{rid}/close", new { outcome = "noBreach" })).StatusCode);
        Assert.Contains(NoteKind.ReportNoBreach, await Kinds(b));
        // Nothing reaches the reported person.
        Assert.DoesNotContain(NoteKind.ReportNoBreach, await Kinds(a));
    }

    [Fact]
    public async Task A_suspension_hides_the_person_stops_them_and_gives_them_the_reasons_until_restored()
    {
        var (a, aId, _) = await PersonAsync("Jana");
        var (b, bId, _) = await PersonAsync("Kira");
        Assert.Contains(aId, await b.GetStringAsync("/api/matches"));
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync("/api/reports", new { about = aId, reason = "scam", note = "asked for money" })).StatusCode);
        var (admin, adminId) = await AdminAsync();

        var bad = await admin.PostAsJsonAsync($"/api/admin/accounts/{aId}/suspend", new { ground = "scam", facts = "too short" });
        Assert.Equal("badFacts", await ErrorAsync(bad));
        Assert.Equal("badGround", await ErrorAsync(await admin.PostAsJsonAsync($"/api/admin/accounts/{aId}/suspend", new { ground = "nope", facts = "Asked another member to send a deposit by bank transfer." })));
        Assert.Equal("notYourself", await ErrorAsync(await admin.PostAsJsonAsync($"/api/admin/accounts/{adminId}/suspend", new { ground = "scam", facts = "Asked another member to send a deposit by bank transfer." })));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/accounts/{aId}/suspend", new { ground = "scam", facts = "Asked another member to send a deposit by bank transfer." })).StatusCode);

        // Hidden from others, stopped at the door, but told why and still able to leave with their data.
        Assert.DoesNotContain(aId, await b.GetStringAsync("/api/matches"));
        var blocked = await a.GetAsync("/api/matches");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("suspended", await ErrorAsync(blocked));
        var me = await a.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.Equal("scam", me.GetProperty("suspension").GetProperty("ground").GetString());
        Assert.Contains("deposit", me.GetProperty("suspension").GetProperty("facts").GetString());
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/api/me/export")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsJsonAsync("/api/contact", new { topic = "appeal", email = "jana@example.com", body = "I'd like to appeal this decision." })).StatusCode);
        // The reporter is told action was taken; the report is closed.
        Assert.Contains(NoteKind.ReportActed, await Kinds(b));
        Assert.True(Db(db => db.Reports.Where(r => r.AboutId == aId).All(r => r.Outcome == "suspended" && r.DoneAt != null)));

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/accounts/{aId}/restore", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/api/matches")).StatusCode);
        Assert.Contains(aId, await b.GetStringAsync("/api/matches"));
    }
}
