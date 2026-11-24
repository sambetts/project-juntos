using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>The house browser (B-064): every home, filtered and paged on the server, scored against your own profile;
/// saved homes are private. Made-up data only.</summary>
public class BrowseTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public BrowseTests(AppFactory app) => _app = app;

    /// <summary>Three homes in Zaragoza (no made-up home is there), one of them gone from the agency's site.</summary>
    async Task<string> SeedAsync()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var now = _app.Clock.GetUtcNow();
        Home H(string n, int price, int beds, int m2, bool gone = false) => new()
        {
            Id = $"tecnocasa:br-{tag}-{n}", Source = "tecnocasa", Agency = "Tecnocasa", Url = $"https://www.tecnocasa.es/venta/piso/zaragoza/{tag}{n}.html",
            Price = price, Beds = beds, M2 = m2, Baths = 1, Type = "flat", Province = "Zaragoza", Municipality = "Zaragoza", Area = "p50",
            FirstSeenAt = now, LastSeenAt = now, CheckedAt = now, GoneAt = gone ? now : null,
        };
        db.Homes.AddRange(H("a", 150_000, 2, 70), H("b", 210_000, 3, 95), H("c", 90_000, 1, 45), H("gone", 120_000, 2, 60, gone: true));
        await db.SaveChangesAsync();
        return tag;
    }

    static IEnumerable<JsonElement> Homes(JsonElement r) => r.GetProperty("homes").EnumerateArray();

    [Fact]
    public async Task Only_signed_in_people_with_a_profile_can_browse()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Browser().GetAsync("/api/browse")).StatusCode);
        var c = _app.Browser();
        await AppFactory.SignUpAsync(c, $"br-n-{Guid.NewGuid():N}@example.com", "Nora", onboard: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/browse")).StatusCode);
    }

    [Fact]
    public async Task Filters_sort_and_pages_on_the_server()
    {
        var tag = await SeedAsync();
        var c = _app.Browser();
        await AppFactory.SignUpAsync(c, $"br-{Guid.NewGuid():N}@example.com", "Pablo");

        var r = await c.GetFromJsonAsync<JsonElement>("/api/browse?areas=p50&sort=priceUp&take=48");
        var mine = Homes(r).Where(h => h.GetProperty("id").GetString()!.Contains(tag)).ToList();
        Assert.Equal(["c", "a", "b"], mine.Select(h => h.GetProperty("id").GetString()![^1..]));   // the gone one never shows
        Assert.All(Homes(r), h => Assert.Equal("p50", h.GetProperty("area").GetString()));
        Assert.All(mine, h => Assert.InRange(h.GetProperty("score").GetInt32(), 0, 100));

        r = await c.GetFromJsonAsync<JsonElement>("/api/browse?areas=p50&pmin=100000&pmax=200000&beds=2");
        Assert.Equal([$"tecnocasa:br-{tag}-a"], Homes(r).Select(h => h.GetProperty("id").GetString()).Where(id => id!.Contains(tag)));

        var page1 = await c.GetFromJsonAsync<JsonElement>("/api/browse?take=2&sort=priceUp");
        var page2 = await c.GetFromJsonAsync<JsonElement>("/api/browse?take=2&skip=2&sort=priceUp");
        Assert.Equal(2, Homes(page1).Count());
        Assert.True(page1.GetProperty("total").GetInt32() >= 3);
        Assert.Empty(Homes(page1).Select(h => h.GetProperty("id").GetString()).Intersect(Homes(page2).Select(h => h.GetProperty("id").GetString())));

        // "My areas" keeps to the areas in the viewer's profile.
        r = await c.GetFromJsonAsync<JsonElement>("/api/browse?mine=true&take=48");
        var areas = r.GetProperty("myAreas").EnumerateArray().Select(a => a.GetString()).ToHashSet();
        Assert.All(Homes(r), h => Assert.Contains(h.GetProperty("area").GetString(), areas));
    }

    [Fact]
    public async Task Saved_homes_are_private_and_go_with_the_account()
    {
        var tag = await SeedAsync();
        var a = _app.Browser(); var b = _app.Browser();
        var aId = await AppFactory.SignUpAsync(a, $"br-a-{Guid.NewGuid():N}@example.com", "Ana");
        await AppFactory.SignUpAsync(b, $"br-b-{Guid.NewGuid():N}@example.com", "Bea");
        var home = $"tecnocasa:br-{tag}-b";

        Assert.Equal(HttpStatusCode.NoContent, (await a.PutAsync($"/api/saved/{Uri.EscapeDataString(home)}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.PutAsync($"/api/saved/{Uri.EscapeDataString(home)}", null)).StatusCode); // twice is fine
        Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsync($"/api/saved/{Uri.EscapeDataString($"tecnocasa:br-{tag}-gone")}", null)).StatusCode);

        var r = await a.GetFromJsonAsync<JsonElement>("/api/browse?saved=true");
        Assert.Equal([home], Homes(r).Select(h => h.GetProperty("id").GetString()));
        Assert.True(Homes(r).Single().GetProperty("saved").GetBoolean());
        Assert.Equal(1, r.GetProperty("savedCount").GetInt32());
        Assert.Empty(Homes(await b.GetFromJsonAsync<JsonElement>("/api/browse?saved=true")));

        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync($"/api/saved/{Uri.EscapeDataString(home)}")).StatusCode);
        Assert.Empty(Homes(await a.GetFromJsonAsync<JsonElement>("/api/browse?saved=true")));

        await a.PutAsync($"/api/saved/{Uri.EscapeDataString(home)}", null);
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync("/api/me")).StatusCode);
        using var scope = _app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDb>().SavedHomes.AnyAsync(s => s.AccountId == aId));
    }
}
