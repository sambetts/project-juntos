using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>The public release: made-up people off, real people only.</summary>
public sealed class RealOnlyFactory : AppFactory
{
    protected override void Configure(IWebHostBuilder builder) => builder.UseSetting("People:Demo", "false");
}

/// <summary>Session 38, D-059: few people at launch. Made-up people off, the early-days flag, "someone compatible joined".</summary>
public class EarlyDaysTests
{
    static int _n;
    static string NewEmail() => $"early{Interlocked.Increment(ref _n)}.{Guid.NewGuid().ToString("N")[..6]}@example.com";

    /// <summary>Signs up, saves a profile, then finishes the wizard, as the app does (the notice goes out at that moment).</summary>
    static async Task<(HttpClient C, string Id)> JoinAsync(AppFactory app, string name, string purpose = "live")
    {
        var c = app.Browser();
        var id = await AppFactory.SignUpAsync(c, NewEmail(), name, onboard: false);
        var p = ToDict(ApiTests.RoomProfile(name)); p["purpose"] = purpose;
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", p)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/me/onboarded", null)).StatusCode);
        return (c, id);
    }

    static Dictionary<string, object> ToDict(object o) =>
        JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(o))!;

    static async Task<JsonElement> Matches(HttpClient c) => await c.GetFromJsonAsync<JsonElement>("/api/matches");

    static async Task<List<(string Kind, string? Actor)>> Notices(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/inbox")).GetProperty("items").EnumerateArray()
            .Select(i => (i.GetProperty("kind").GetString()!, i.GetProperty("actor").GetString())).ToList();

    [Fact]
    public async Task With_made_up_people_off_a_newcomer_meets_only_real_people_and_sees_the_early_days_flag()
    {
        using var app = new RealOnlyFactory();
        var (a, _) = await JoinAsync(app, "Ana");
        var m = await Matches(a);
        Assert.Empty(m.GetProperty("compatible").EnumerateArray());
        Assert.False(m.GetProperty("demoPeople").GetBoolean());
        Assert.True(m.GetProperty("early").GetBoolean());

        using var s = app.Services.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<AppDb>();
        Assert.Equal(0, await db.Accounts.CountAsync(x => x.Kind != AccountKind.User));
    }

    [Fact]
    public async Task Turning_made_up_people_off_removes_them_with_their_likes()
    {
        using var app = new AppFactory();
        var (a, aId) = await JoinAsync(app, "Ana");
        var m = await Matches(a);
        Assert.True(m.GetProperty("demoPeople").GetBoolean());
        var demoId = m.GetProperty("compatible").EnumerateArray().First(x => x.GetProperty("person").GetProperty("isDemo").GetBoolean())
            .GetProperty("person").GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await a.PutAsJsonAsync($"/api/reactions/{demoId}", new { kind = "like" })).StatusCode);

        using (var s = app.Services.CreateScope())
            Assert.True(await s.ServiceProvider.GetRequiredService<DemoData>().RemoveAllAsync() > 0);

        using (var s = app.Services.CreateScope())
        {
            var db = s.ServiceProvider.GetRequiredService<AppDb>();
            Assert.Equal(0, await db.Accounts.CountAsync(x => x.Kind != AccountKind.User));
            Assert.Equal(0, await db.Reactions.CountAsync(r => r.FromId == aId));
        }
        m = await Matches(a);
        Assert.Empty(m.GetProperty("compatible").EnumerateArray());
        Assert.False(m.GetProperty("demoPeople").GetBoolean());
    }

    [Fact]
    public async Task A_newcomer_is_announced_to_compatible_real_people_only()
    {
        using var app = new RealOnlyFactory();
        var (a, _) = await JoinAsync(app, "Ana");
        var (let, _) = await JoinAsync(app, "Lola", purpose: "let"); // a different purpose never matches
        var (b, bId) = await JoinAsync(app, "Bea");

        Assert.Equal([("nearby", (string?)bId)], await Notices(a));
        Assert.Empty(await Notices(let));
        Assert.Empty(await Notices(b)); // the newcomer isn't told about herself, nor about people already there
        var inbox = await a.GetFromJsonAsync<JsonElement>("/api/inbox");
        Assert.Equal("Bea", inbox.GetProperty("people").GetProperty(bId).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Nobody_holds_more_than_three_unread_nearby_notices_and_blocks_are_respected()
    {
        using var app = new RealOnlyFactory();
        var (a, aId) = await JoinAsync(app, "Ana");
        for (var i = 0; i < 4; i++) await JoinAsync(app, "Persona" + i);
        Assert.Equal(Api.Api.MaxNearbyUnread, (await Notices(a)).Count(n => n.Kind == "nearby"));

        // Reading them makes room again.
        await a.PostAsync("/api/inbox/read", null);
        var (e, eId) = await JoinAsync(app, "Eva");
        Assert.Contains(("nearby", (string?)eId), await Notices(a));

        // Someone Ana has blocked is never announced to her (and she isn't to them).
        var other = app.Browser();
        var oId = await AppFactory.SignUpAsync(other, NewEmail(), "Olga", onboard: false);
        Assert.Equal(HttpStatusCode.OK, (await other.PutAsJsonAsync("/api/profile", ApiTests.RoomProfile("Olga"))).StatusCode);
        using (var s = app.Services.CreateScope())
        {
            var db = s.ServiceProvider.GetRequiredService<AppDb>();
            db.Blocks.Add(new Block { FromId = aId, ToId = oId, At = app.Clock.GetUtcNow() });
            await db.SaveChangesAsync();
        }
        await other.PostAsync("/api/me/onboarded", null);
        Assert.DoesNotContain(await Notices(a), n => n.Actor == oId);
    }

    [Fact]
    public async Task The_early_days_flag_clears_once_five_real_compatible_people_are_left_to_meet()
    {
        using var app = new RealOnlyFactory();
        var (a, _) = await JoinAsync(app, "Ana");
        var ids = new List<string>();
        for (var i = 0; i < 4; i++) ids.Add((await JoinAsync(app, "Persona" + i)).Id);
        Assert.True((await Matches(a)).GetProperty("early").GetBoolean());
        ids.Add((await JoinAsync(app, "Quinta")).Id);
        Assert.False((await Matches(a)).GetProperty("early").GetBoolean());
        // Passing on someone takes them out of the count; liking them doesn't.
        await a.PutAsJsonAsync($"/api/reactions/{ids[0]}", new { kind = "like" });
        Assert.False((await Matches(a)).GetProperty("early").GetBoolean());
        await a.PutAsJsonAsync($"/api/reactions/{ids[1]}", new { kind = "pass" });
        Assert.True((await Matches(a)).GetProperty("early").GetBoolean());
    }
}
