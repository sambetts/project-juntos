using System.Net;
using System.Net.Http.Json;
using Compicasa.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>The free release's funnel (B-074): counts of real people only, shown only to listed admins. Made-up data only.</summary>
public class FunnelTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public FunnelTests(AppFactory app) => _app = app;

    async Task<FunnelView> CountAsync()
    {
        using var scope = _app.Services.CreateScope();
        return await Funnel.ComputeAsync(scope.ServiceProvider.GetRequiredService<AppDb>(), _app.Clock.GetUtcNow());
    }

    [Fact]
    public void Only_listed_accounts_are_admins()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:AccountIds"] = " a1 , b2" }).Build();
        Assert.True(Funnel.IsAdmin(cfg, "a1"));
        Assert.True(Funnel.IsAdmin(cfg, "b2"));
        Assert.False(Funnel.IsAdmin(cfg, "c3"));
        Assert.False(Funnel.IsAdmin(cfg, null));
        Assert.False(Funnel.IsAdmin(new ConfigurationBuilder().Build(), "a1"));
    }

    [Fact]
    public async Task Anyone_not_listed_gets_nothing()
    {
        var c = _app.Browser();
        await AppFactory.SignUpAsync(c, $"funnel-{Guid.NewGuid():N}@example.com", "Ines");
        var r = await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/admin/funnel");
        Assert.False(r.GetProperty("allowed").GetBoolean());
        Assert.False(r.TryGetProperty("funnel", out _));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Browser().GetAsync("/api/admin/funnel")).StatusCode);
    }

    [Fact]
    public async Task Counts_real_people_and_matches_between_real_people_only()
    {
        var before = await CountAsync();
        var a = _app.Browser(); var b = _app.Browser(); var late = _app.Browser();
        var aId = await AppFactory.SignUpAsync(a, $"funnel-a-{Guid.NewGuid():N}@example.com", "Ana");
        var bId = await AppFactory.SignUpAsync(b, $"funnel-b-{Guid.NewGuid():N}@example.com", "Bea");
        await AppFactory.SignUpAsync(late, $"funnel-c-{Guid.NewGuid():N}@example.com", "Cris", onboard: false);

        // Ana likes a made-up person too: that is a like, but never a match in the funnel.
        var madeUp = (await a.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/matches")).GetProperty("compatible")[0].GetProperty("person").GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await a.PutAsJsonAsync($"/api/reactions/{madeUp}", new { kind = "like" })).StatusCode);
        var mid = await CountAsync();
        Assert.Equal(before.All.SignedUp + 3, mid.All.SignedUp);
        Assert.Equal(before.All.Onboarded + 2, mid.All.Onboarded);
        Assert.Equal(before.All.Liked + 1, mid.All.Liked);
        Assert.Equal(before.All.Matched, mid.All.Matched);

        Assert.Equal(HttpStatusCode.OK, (await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "like" })).StatusCode);
        var after = await CountAsync();
        Assert.Equal(before.All.Matched + 2, after.All.Matched);
        Assert.Equal(before.Weeks.Sum(w => w.Matches) + 1, after.Weeks.Sum(w => w.Matches));
        Assert.Equal(8, after.Weeks.Count);
        // Nobody here has been in for 14 days yet, so the 14-day measure doesn't count them.
        Assert.Equal(before.All.Cohort14, after.All.Cohort14);
        Assert.True(after.Madrid.SignedUp <= after.All.SignedUp);
    }
}
