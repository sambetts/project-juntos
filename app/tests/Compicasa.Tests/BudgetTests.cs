using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Core;

namespace Compicasa.Tests;

/// <summary>Maximum budgets (B-058, D-044): each household's budget is private; a group's total filters its homes, never people. Made-up data only.</summary>
public class BudgetTests
{
    static HouseBrief Brief() => new(2, 1, 0, 1, 0, 0, 0, 2, ["south"], ["flat"], [], [], 70, 60, 80, false);

    static HomeListing Listing(int? price) =>
        new("t:1", "demo", "Demo", null, price, 70, 2, 1, "flat", "Getafe", "28901", null, "south", [], "E", true);

    [Fact]
    public void A_home_over_the_combined_budget_fails_and_one_within_it_passes()
    {
        Assert.True(HomeFit.Fit(Brief(), 60, 80, Listing(250_000), 250_000).Ok);
        Assert.DoesNotContain("price", HomeFit.Fit(Brief(), 60, 80, Listing(250_000), 250_000).Fits); // every home shown is within it
        var over = HomeFit.Fit(Brief(), 60, 80, Listing(250_001), 250_000);
        Assert.False(over.Ok);
        Assert.Contains("price", over.Misses);
        // No price in the advert: left for the group to check, not hidden.
        var unknown = HomeFit.Fit(Brief(), 60, 80, Listing(null), 250_000);
        Assert.True(unknown.Ok);
        Assert.Contains("price", unknown.Check);
        // No budget: price plays no part, as before.
        Assert.Equal(HomeFit.Fit(Brief(), 60, 80, Listing(900_000)), HomeFit.Fit(Brief(), 60, 80, Listing(900_000), null), new FitComparer());
        Assert.True(HomeFit.Fit(Brief(), 60, 80, Listing(900_000)).Ok);
    }

    [Fact]
    public void The_total_needs_everyone_s_budget()
    {
        Assert.Equal(300_000, Budgets.Combined([new Profile { MaxBudget = 120_000 }, new Profile { MaxBudget = 180_000 }]));
        Assert.Null(Budgets.Combined([new Profile { MaxBudget = 120_000 }, new Profile()]));
        Assert.Null(Budgets.Combined([]));
    }

    [Fact]
    public void Made_up_people_all_have_a_steady_budget()
    {
        Assert.All(Seeds.Profiles(), p => Assert.InRange(p.MaxBudget ?? 0, Budgets.Min, Budgets.Max));
        var day = new DateOnly(2026, 11, 23);
        var a = DemoGenerator.Batch(day);
        Assert.All(a, p => Assert.InRange(p.MaxBudget ?? 0, 130_000, 500_000));
        Assert.Equal(a.Select(p => p.MaxBudget), DemoGenerator.Batch(day).Select(p => p.MaxBudget));
    }

    sealed class FitComparer : IEqualityComparer<HomeFitResult>
    {
        public bool Equals(HomeFitResult? x, HomeFitResult? y) => x!.Ok == y!.Ok && x.Score == y.Score
            && x.Fits.SequenceEqual(y.Fits) && x.Misses.SequenceEqual(y.Misses) && x.Check.SequenceEqual(y.Check);
        public int GetHashCode(HomeFitResult o) => o.Score;
    }
}

public class BudgetApiTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public BudgetApiTests(AppFactory app) => _app = app;

    static object Profile(string name, int? maxBudget) => new
    {
        name, household = "single", bedrooms = 1, privateBath = false, cars = 0, pets = Array.Empty<string>(), acceptsPets = true,
        smoking = "no", acceptsSmoking = "outside", sleep = 3, tidy = 3, social = 3, guests = 3, meals = 3, wfh = false, horizon = "5-9",
        areas = new[] { "centre", "city", "north", "south", "east" }, types = new[] { "flat", "house" },
        needs = new Dictionary<string, string>(), open = new[] { "single", "couple", "family", "singleParent" }, maxHouseholds = 4, maxBudget,
    };

    async Task<(HttpClient C, string Id)> Person(string name, int? maxBudget)
    {
        var c = _app.Browser();
        var id = await AppFactory.SignUpAsync(c, $"budget-{Guid.NewGuid():N}@example.com", name);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", Profile(name, maxBudget))).StatusCode);
        return (c, id);
    }

    [Fact]
    public async Task Budgets_are_checked_and_kept_private()
    {
        var (c, _) = await Person("Rosa", 150_000);
        Assert.Equal(150_000, (await c.GetFromJsonAsync<JsonElement>("/api/profile")).GetProperty("maxBudget").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/profile", Profile("Rosa", 5_000))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/profile", Profile("Rosa", 6_000_000))).StatusCode);
        // Clearing it is allowed: then prices aren't filtered.
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", Profile("Rosa", null))).StatusCode);
        var raw = await c.GetStringAsync("/api/matches");
        Assert.DoesNotContain("maxBudget", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_group_sees_only_homes_within_its_combined_budget()
    {
        var (a, aId) = await Person("Ana", 110_000);
        var (b, bId) = await Person("Bea", null);
        await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" });
        await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "like" });
        var gid = (await (await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        // Bea hasn't set one: no filter, and the group is told who is missing (not anyone's figure).
        var before = await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/homes");
        Assert.Equal(JsonValueKind.Null, before.GetProperty("budget").ValueKind);
        Assert.Equal([bId], before.GetProperty("budgetUnset").EnumerateArray().Select(x => x.GetString()));
        var all = before.GetProperty("homes").GetArrayLength();

        await b.PutAsJsonAsync("/api/profile", Profile("Bea", 140_000));
        var after = await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/homes");
        Assert.Equal(250_000, after.GetProperty("budget").GetInt32());
        Assert.Empty(after.GetProperty("budgetUnset").EnumerateArray());
        var homes = after.GetProperty("homes").EnumerateArray().ToList();
        Assert.All(homes, h => Assert.True(h.GetProperty("price").ValueKind == JsonValueKind.Null || h.GetProperty("price").GetInt32() <= 250_000));
        Assert.True(after.GetProperty("overBudget").GetInt32() > 0, "the made-up homes include some over 250,000 €");
        Assert.True(homes.Count < all);

        // The Home page filters too, but doesn't show the pair's total (it would give away the other person's figure).
        var mine = await a.GetFromJsonAsync<JsonElement>("/api/me/homes");
        Assert.False(mine.TryGetProperty("budget", out _));
        Assert.True(mine.GetProperty("budgetSet").GetBoolean());
    }
}
