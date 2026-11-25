using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Compicasa.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Compicasa.Tests;

/// <summary>An app on a throwaway SQLite file, with a fixed invite code and a clock the test controls.</summary>
public sealed class AppFactory : WebApplicationFactory<Program>
{
    public const string Invite = "test-invite";
    readonly string _db = Path.Combine(Path.GetTempPath(), $"compicasa-test-{Guid.NewGuid():N}.db");
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Db", $"Data Source={_db}");
        builder.UseSetting("Auth:InviteCode", Invite);
        builder.UseSetting("Auth:AttemptsPerMinute", "1000");
        builder.UseSetting("Contact:PerHour", "1000");
        builder.UseSetting("Demo:RefreshDaily", "false");
        // Tests change homes and read them back at once; HomeIndex's copy has its own test.
        builder.UseSetting("Homes:CacheSeconds", "0");
        builder.ConfigureServices(s => s.AddSingleton<TimeProvider>(Clock));
    }

    public HttpClient Browser()
    {
        var c = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        c.DefaultRequestHeaders.Add(Api.Api.CsrfHeader, "1");
        return c;
    }

    /// <summary>Signs up with email and password (local mode), the 18+ box and today's terms; then finishes onboarding unless told not to.</summary>
    public static async Task<string> SignUpAsync(HttpClient c, string email, string name = "Tester", bool onboard = true)
    {
        var r = await c.PostAsJsonAsync("/api/auth/signup", new { email, password = "a-long-password", name, inviteCode = Invite, adult = true, terms = Legal.TermsVersion });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var me = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(me.GetProperty("onboarded").GetBoolean());
        if (onboard) Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/me/onboarded", null)).StatusCode);
        return me.GetProperty("id").GetString()!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_db); } catch (IOException) { }
    }
}

public class ApiTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public ApiTests(AppFactory app) => _app = app;

    static int _n;
    static string NewEmail() => $"tester{Interlocked.Increment(ref _n)}.{Guid.NewGuid().ToString("N")[..6]}@example.com";

    async Task<(HttpClient C, string Id)> SignUpAsync(string? name = null, bool onboard = true)
    {
        var c = _app.Browser();
        return (c, await AppFactory.SignUpAsync(c, NewEmail(), name ?? "Tester", onboard));
    }

    static object Signup(string email, string password = "a-long-password", string invite = AppFactory.Invite, bool adult = true, string? terms = null) =>
        new { email, password, name = "X", inviteCode = invite, adult, terms = terms ?? Legal.TermsVersion };

    /// <summary>The prototype's "room" example: one room, open to anyone, transport a must.</summary>
    internal static object RoomProfile(string name) => new
    {
        name, household = "single", bedrooms = 1, privateBath = false, cars = 0, pets = Array.Empty<string>(), acceptsPets = true,
        smoking = "no", acceptsSmoking = "outside", sleep = 3, tidy = 3, social = 3, guests = 3, meals = 3, wfh = false, horizon = "5-9",
        areas = new[] { "centre", "city", "north", "south", "east" }, types = new[] { "flat", "house" },
        needs = new Dictionary<string, string> { ["transport"] = "must" }, open = new[] { "single", "couple", "family", "singleParent" }, maxHouseholds = 4,
    };

    [Fact]
    public async Task Health_endpoints_answer()
    {
        var c = _app.Browser();
        Assert.Equal("ok", await c.GetStringAsync("/healthz"));
        Assert.Equal("ready", await c.GetStringAsync("/healthz/ready"));
    }

    [Fact]
    public async Task Sign_up_needs_the_invite_code_a_sound_password_18_plus_and_the_terms()
    {
        var c = _app.Browser();
        async Task<string> Code(object body)
        {
            var r = await c.PostAsJsonAsync("/api/auth/signup", body);
            return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()!;
        }
        Assert.Equal("badInvite", await Code(Signup(NewEmail(), invite: "wrong")));
        Assert.Equal("badPassword", await Code(Signup(NewEmail(), password: "short")));
        Assert.Equal("badEmail", await Code(Signup("not-an-email")));
        Assert.Equal("needAdult", await Code(Signup(NewEmail(), adult: false)));
        Assert.Equal("needTerms", await Code(Signup(NewEmail(), terms: "an-old-version")));
        var e = NewEmail();
        Assert.Equal(HttpStatusCode.OK, (await _app.Browser().PostAsJsonAsync("/api/auth/signup", Signup(e))).StatusCode);
        Assert.Equal("emailTaken", await Code(Signup(e.ToUpperInvariant())));
        // What they agreed to, and when, is kept with the account.
        using var scope = _app.Services.CreateScope();
        var acc = scope.ServiceProvider.GetRequiredService<AppDb>().Accounts.Single(a => a.Email == e);
        Assert.Equal(Legal.TermsVersion, acc.TermsVersion);
        Assert.NotNull(acc.TermsAcceptedAt);
        Assert.NotNull(acc.AdultConfirmedAt);
    }

    [Fact]
    public async Task Until_onboarding_is_finished_you_see_no_one_and_no_one_sees_you()
    {
        var (fresh, freshId) = await SignUpAsync("Nueva", onboard: false);
        Assert.Equal(HttpStatusCode.Conflict, (await fresh.GetAsync("/api/matches")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fresh.PutAsJsonAsync("/api/reactions/marta", new { kind = "like" })).StatusCode);
        var (other, _) = await SignUpAsync("Otra");
        var m = await other.GetFromJsonAsync<JsonElement>("/api/matches");
        Assert.False(m.GetProperty("people").TryGetProperty(freshId, out _));
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/reactions/{freshId}", new { kind = "like" })).StatusCode);
        // An empty area list can't finish onboarding.
        var noAreas = ToDict(RoomProfile("Nueva")); noAreas["areas"] = Array.Empty<string>();
        Assert.Equal(HttpStatusCode.OK, (await fresh.PutAsJsonAsync("/api/profile", noAreas)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fresh.PostAsync("/api/me/onboarded", null)).StatusCode);
        await fresh.PutAsJsonAsync("/api/profile", RoomProfile("Nueva"));
        var me = await (await fresh.PostAsync("/api/me/onboarded", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(me.GetProperty("onboarded").GetBoolean());
        m = await other.GetFromJsonAsync<JsonElement>("/api/matches");
        Assert.True(m.GetProperty("people").TryGetProperty(freshId, out _));
    }

    [Fact]
    public async Task Config_tells_the_front_end_how_to_sign_in()
    {
        var cfg = await _app.Browser().GetFromJsonAsync<JsonElement>("/api/config");
        Assert.Equal("local", cfg.GetProperty("auth").GetString());
        Assert.True(cfg.GetProperty("invite").GetBoolean());
        Assert.Equal(Legal.TermsVersion, cfg.GetProperty("terms").GetString());
    }

    [Fact]
    public async Task Log_in_log_out_and_wrong_password()
    {
        var c = _app.Browser(); var u = NewEmail();
        await AppFactory.SignUpAsync(c, u, "Ana");
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/me")).StatusCode);
        await c.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/login", new { email = u, password = "wrong-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/login", new { email = " " + u.ToUpperInvariant(), password = "a-long-password" })).StatusCode);
        // Passwords are stored hashed, never as typed.
        using var scope = _app.Services.CreateScope();
        var acc = scope.ServiceProvider.GetRequiredService<AppDb>().Accounts.Single(a => a.Email == u);
        Assert.DoesNotContain("a-long-password", acc.PasswordHash);
    }

    [Fact]
    public async Task Changing_requests_without_the_header_are_refused()
    {
        var (c, _) = await SignUpAsync();
        var raw = _app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var r = await raw.PutAsJsonAsync("/api/reactions/marta", new { kind = "like" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Browser().GetAsync("/api/matches")).StatusCode);
    }

    [Fact]
    public async Task Profile_is_saved_and_unknown_values_are_refused()
    {
        var (c, _) = await SignUpAsync();
        var ok = await c.PutAsJsonAsync("/api/profile", RoomProfile("Rosa"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var p = await c.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal("Rosa", p.GetProperty("name").GetString());
        Assert.Equal("must", p.GetProperty("needs").GetProperty("transport").GetString());
        var bad = await c.PutAsJsonAsync("/api/profile", new Dictionary<string, object>(((IDictionary<string, object>)ToDict(RoomProfile("Rosa")))) { ["areas"] = new[] { "mars" } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    static IDictionary<string, object> ToDict(object o) => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(o))!;

    [Fact]
    public async Task Matches_hide_other_peoples_private_settings()
    {
        var (c, _) = await SignUpAsync();
        await c.PutAsJsonAsync("/api/profile", RoomProfile("Rosa"));
        var raw = await c.GetStringAsync("/api/matches");
        var m = JsonDocument.Parse(raw).RootElement;
        Assert.True(m.GetProperty("compatible").GetArrayLength() > 0);
        var first = m.GetProperty("compatible")[0].GetProperty("person");
        foreach (var secret in new[] { "pickiness", "open", "acceptsSmoking", "acceptsPets", "maxHouseholds", "sleep", "tidy", "maxBudget" })
            Assert.False(first.TryGetProperty(secret, out _), secret + " leaked");
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Liking_a_made_up_person_gets_a_simulated_reply_and_groups_form()
    {
        var (c, me) = await SignUpAsync();
        await c.PutAsJsonAsync("/api/profile", RoomProfile("Rosa"));
        var m = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        // Like everyone compatible, as the prototype's like=all hook did.
        foreach (var x in m.GetProperty("compatible").EnumerateArray())
            await c.PutAsJsonAsync($"/api/reactions/{x.GetProperty("person").GetProperty("id").GetString()}", new { kind = "like" });
        m = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        Assert.True(m.GetProperty("mutualCount").GetInt32() > 0);
        var sugg = m.GetProperty("suggestions");
        Assert.True(sugg.GetArrayLength() > 0);
        var ids = sugg[0].GetProperty("ids").EnumerateArray().Select(e => e.GetString()).ToArray();

        var preview = await c.PostAsJsonAsync("/api/preview", new { ids });
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var created = await c.PostAsJsonAsync("/api/groups", new { ids });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var g = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("formed", g.GetProperty("state").GetString());
        Assert.True(g.GetProperty("check").GetProperty("ok").GetBoolean());
        Assert.True(g.GetProperty("brief").GetProperty("beds").GetInt32() >= 2);
        var list = await c.GetFromJsonAsync<JsonElement>("/api/groups");
        Assert.Equal(1, list.GetArrayLength());

        var resize = await c.PutAsJsonAsync($"/api/groups/{g.GetProperty("id").GetString()}/size", new { min = 120, max = 90 });
        Assert.Equal(HttpStatusCode.OK, resize.StatusCode);
        var detail = await c.GetFromJsonAsync<JsonElement>($"/api/groups/{g.GetProperty("id").GetString()}");
        Assert.Equal(90, detail.GetProperty("size").GetProperty("min").GetInt32());
    }

    [Fact]
    public async Task A_group_needs_mutual_matches()
    {
        var (c, _) = await SignUpAsync();
        await c.PutAsJsonAsync("/api/profile", RoomProfile("Rosa"));
        var r = await c.PostAsJsonAsync("/api/groups", new { ids = new[] { "diego" } });
        Assert.Equal((HttpStatusCode)422, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(body.GetProperty("issues").EnumerateArray(), i => i.GetProperty("key").GetString() == "notMutual");
    }

    [Fact]
    public async Task Two_real_people_match_only_when_both_say_yes_and_answer_invitations_themselves()
    {
        var (a, aId) = await SignUpAsync("Ana");
        var (b, bId) = await SignUpAsync("Bea");
        await a.PutAsJsonAsync("/api/profile", RoomProfile("Ana"));
        await b.PutAsJsonAsync("/api/profile", RoomProfile("Bea"));
        var r1 = await (await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(r1.GetProperty("mutual").GetBoolean());
        var r2 = await (await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "like" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(r2.GetProperty("mutual").GetBoolean());

        var g = await (await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pending", g.GetProperty("state").GetString());
        var gid = g.GetProperty("id").GetString();
        var inv = await b.GetFromJsonAsync<JsonElement>("/api/groups");
        Assert.Equal("pending", inv[0].GetProperty("myStatus").GetString());
        var accepted = await (await b.PostAsJsonAsync($"/api/groups/{gid}/respond", new { accept = true })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("formed", accepted.GetProperty("state").GetString());

        // A third person can't read the group.
        var (outsider, _) = await SignUpAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/groups/{gid}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_an_account_removes_it()
    {
        var (c, id) = await SignUpAsync();
        await c.PutAsJsonAsync("/api/reactions/marta", new { kind = "like" });
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/me")).StatusCode);
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        Assert.False(db.Accounts.Any(a => a.Id == id));
        Assert.False(db.Reactions.Any(r => r.FromId == id));
    }

    [Fact]
    public async Task Security_headers_are_sent()
    {
        var r = await _app.Browser().GetAsync("/healthz");
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("noindex", r.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Contains("frame-ancestors 'none'", r.Headers.GetValues("Content-Security-Policy").Single());
    }
}

public class DemoRefreshTests
{
    [Fact]
    public void A_days_batch_is_made_up_valid_and_repeatable()
    {
        var day = new DateOnly(2026, 11, 22);
        var a = DemoGenerator.Batch(day); var b = DemoGenerator.Batch(day);
        Assert.Equal(DemoGenerator.BatchSize, a.Count);
        Assert.Equal(a.Select(p => p.Name), b.Select(p => p.Name));
        Assert.NotEqual(a.Select(p => p.Name), DemoGenerator.Batch(day.AddDays(1)).Select(p => p.Name));
        foreach (var p in a)
        {
            var (clean, problem) = ProfileRules.Clean(p, p);
            Assert.True(problem is null, $"{p.Id}: {problem}");
            Assert.StartsWith("gen-20261122-", p.Id);
            Assert.Contains(p.Household, p.Open);
            Assert.False(string.IsNullOrWhiteSpace(p.Bio?.Es));
            Assert.False(string.IsNullOrWhiteSpace(p.Bio?.En));
        }
    }

    [Fact]
    public async Task The_batch_is_replaced_each_morning_and_its_likes_and_groups_go_with_it()
    {
        using var app = new AppFactory();
        var c = app.Browser();
        await AppFactory.SignUpAsync(c, "refresh1@example.com", "R");
        int Count(string prefix) { using var s = app.Services.CreateScope(); return s.ServiceProvider.GetRequiredService<AppDb>().Accounts.Count(a => a.Id.StartsWith(prefix)); }
        Assert.Equal(DemoGenerator.BatchSize, Count("gen-20261122-"));
        Assert.Equal(14, Count("") - Count("gen-") - Count("u-"));
        await c.PutAsJsonAsync("/api/reactions/gen-20261122-01", new { kind = "like" });

        // 03:30 Madrid on the 23rd: not yet due. 04:30: due.
        app.Clock.SetUtcNow(new DateTimeOffset(2026, 11, 23, 2, 30, 0, TimeSpan.Zero));
        using (var s = app.Services.CreateScope()) Assert.False(await s.ServiceProvider.GetRequiredService<DemoData>().RefreshIfDueAsync());
        app.Clock.SetUtcNow(new DateTimeOffset(2026, 11, 23, 3, 30, 0, TimeSpan.Zero));
        using (var s = app.Services.CreateScope()) Assert.True(await s.ServiceProvider.GetRequiredService<DemoData>().RefreshIfDueAsync());

        Assert.Equal(0, Count("gen-20261122-"));
        Assert.Equal(DemoGenerator.BatchSize, Count("gen-20261123-"));
        using (var s = app.Services.CreateScope())
            Assert.False(s.ServiceProvider.GetRequiredService<AppDb>().Reactions.Any(r => r.ToId.StartsWith("gen-20261122-")));
        // The 14 hand-written profiles and the real account stay.
        Assert.Equal(1, Count("u-"));
        Assert.Equal(14 + DemoGenerator.BatchSize + 1, Count(""));
    }
}
