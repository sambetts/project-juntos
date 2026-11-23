using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Core;

namespace Compicasa.Tests;

/// <summary>Live in it or rent it out (B-063): people match only with the same purpose; letting skips the living-together rules. Made-up data only.</summary>
public class PurposeTests
{
    static Profile P(string id, string purpose = Purposes.Live) => new()
    {
        Id = id, Name = id, Purpose = purpose, Areas = ["centre", "city"], Types = ["flat"], Horizon = "5-9",
    };

    [Fact]
    public void People_with_different_purposes_never_match_either_way()
    {
        var live = P("a"); var let = P("b", Purposes.Let);
        var h = Matching.Hard(live, let);
        Assert.False(h.Ok);
        Assert.Equal("purposeDiff", Assert.Single(h.Reasons).Key);
        Assert.Equal(Purposes.Let, h.Reasons[0].Args[0]);
        Assert.False(Matching.Hard(let, live).Ok);
        Assert.False(Matching.Accepts(let, live));
        Assert.False(Matching.GroupCheck([live, let]).Ok);
        Assert.True(Matching.Hard(P("c"), P("d")).Ok);
        Assert.True(Matching.Hard(P("e", Purposes.Let), P("f", Purposes.Let)).Ok);
    }

    [Fact]
    public void Letting_skips_the_living_together_dealbreakers()
    {
        Profile Picky(string id, string purpose)
        {
            var p = P(id, purpose);
            p.Household = id == "x" ? "family" : "single"; p.Open = ["single"]; p.Kids = id == "x" ? 2 : 0;
            p.Smoking = "inside"; p.AcceptsSmoking = "none"; p.Pets = ["dog"]; p.AcceptsPets = false; p.Bedrooms = 4;
            return p;
        }
        Assert.False(Matching.Hard(Picky("x", Purposes.Live), Picky("y", Purposes.Live)).Ok);
        var x = Picky("x", Purposes.Let); var y = Picky("y", Purposes.Let);
        Assert.True(Matching.Hard(x, y).Ok);
        Assert.True(Matching.Hard(y, x).Ok);
        Assert.True(Matching.GroupCheck([x, y]).Ok); // 8 bedrooms between them is fine: they don't live there
        // Area and home type still matter.
        y.Areas = ["east"];
        Assert.Equal("noArea", Assert.Single(Matching.Hard(x, y).Reasons).Key);
    }

    [Fact]
    public void A_letting_score_ignores_how_people_live()
    {
        var a = P("a", Purposes.Let); var b = P("b", Purposes.Let);
        var s = Matching.Score(a, b);
        Assert.Equal(["place", "plans", "home"], s.Parts.Keys);
        Assert.Equal(95, s.Total); // place 1 x 0.4 + plans 1 x 0.4 + no wish-lists 0.75 x 0.2
        b.Sleep = 5; b.Tidy = 1; b.Social = 5; b.Guests = 5; b.Meals = 1; b.Wfh = true; a.Wfh = true; b.Smoking = "inside";
        Assert.Equal(s.Total, Matching.Score(a, b).Total);
        Assert.Equal(Matching.Score(a, b).Total, Matching.Score(b, a).Total);
        b.Horizon = "10+";
        Assert.Equal(79, Matching.Score(a, b).Total); // plans 0.6
        var ex = Matching.Explain(a, b);
        Assert.DoesNotContain(ex.Clicks.Concat(ex.Talks), m => m.Key is "lateHours" or "veryEarly" or "tidier" or "relaxed" or "smokesIn" or "youWfh" or "theyWfh");
        // Budgets never feed matching (D-044), on either path.
        a.MaxBudget = 100_000; b.MaxBudget = 900_000;
        Assert.Equal(79, Matching.Score(a, b).Total); // plans 0.6
    }

    [Fact]
    public void A_letting_group_s_brief_is_the_home_itself()
    {
        var a = P("a", Purposes.Let); a.Bedrooms = 2; a.Cars = 2; a.Wfh = true; a.PrivateBath = true; a.Needs["lift"] = "must";
        var b = P("b", Purposes.Let); b.Bedrooms = 3; b.Kids = 2; b.Household = "family"; b.Adults = 2; b.Pets = ["dog"]; b.Needs["terrace"] = "nice";
        var hb = Matching.Brief([a, b]);
        Assert.Equal(3, hb.Beds); // the most anyone asked for: each answer is "at least"
        Assert.Equal(1, hb.Baths);
        Assert.Equal((0, 0, 0, 0), (hb.Cars, hb.Desks, hb.Kids, hb.PrivateBaths));
        Assert.Empty(hb.Extras);
        Assert.Equal(["terrace", "lift"], hb.Feats.Select(f => f.Id));
        Assert.Equal(["centre", "city"], hb.Areas);
        Assert.Equal(95, hb.M2); // (3 x 12 + 5 + 42) x 1.15 = 95.45
        Assert.Empty(Matching.RoomPlan([a, b]));
        Assert.Empty(Matching.PlacementTips([a, b], "a"));
        Assert.Equal(["agLetKind", "agLetManage", "agLetMoney", "agDecisions", "agExit"], Matching.AgreementTopics([a, b]).Select(t => t.Key));
        // The same two buying to live in get the usual brief.
        a.Purpose = b.Purpose = Purposes.Live;
        Assert.Equal(5, Matching.Brief([a, b]).Beds);
        Assert.NotEmpty(Matching.RoomPlan([a, b]));
    }

    [Fact]
    public void Made_up_people_the_prototype_ones_live_and_about_a_quarter_of_each_batch_lets()
    {
        Assert.All(Seeds.Profiles(), p => Assert.Equal(Purposes.Live, p.Purpose));
        var day = new DateOnly(2026, 11, 23);
        var batch = DemoGenerator.Batch(day);
        var lets = batch.Where(p => p.IsLet()).ToList();
        Assert.InRange(lets.Count, 3, 15);
        Assert.Equal(batch.Select(p => p.Purpose), DemoGenerator.Batch(day).Select(p => p.Purpose));
        Assert.All(lets, p => Assert.Matches("alquil", p.Bio!.Es));
        // Most made-up people buying to let can match someone else in the batch (some look in another city).
        Assert.True(lets.Count(p => lets.Any(q => q.Id != p.Id && Matching.Hard(p, q).Ok)) * 2 > lets.Count);
    }

    [Fact]
    public void A_profile_without_a_purpose_lives_there()
    {
        var p = JsonSerializer.Deserialize<Profile>("""{"name":"Old","household":"single"}""", Seeds.Json)!;
        Assert.Equal(Purposes.Live, p.Purpose);
        Assert.Contains("\"purpose\":\"live\"", JsonSerializer.Serialize(p, Seeds.Json));
        Assert.DoesNotContain("isLet", JsonSerializer.Serialize(p, Seeds.Json), StringComparison.OrdinalIgnoreCase);
    }
}

public class PurposeApiTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public PurposeApiTests(AppFactory app) => _app = app;

    static object Profile(string name, string? purpose) => new
    {
        name, household = "single", bedrooms = 2, privateBath = false, cars = 0, pets = Array.Empty<string>(), acceptsPets = true,
        smoking = "no", acceptsSmoking = "outside", sleep = 3, tidy = 3, social = 3, guests = 3, meals = 3, wfh = false, horizon = "5-9",
        areas = new[] { "centre", "city", "north", "south", "east" }, types = new[] { "flat", "house" },
        needs = new Dictionary<string, string>(), open = new[] { "single", "couple", "family", "singleParent" }, maxHouseholds = 4,
        maxBudget = 200_000, purpose,
    };

    async Task<(HttpClient C, string Id)> Person(string name, string purpose)
    {
        var c = _app.Browser();
        var id = await AppFactory.SignUpAsync(c, $"purpose-{Guid.NewGuid():N}@example.com", name);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", Profile(name, purpose))).StatusCode);
        return (c, id);
    }

    [Fact]
    public async Task Purpose_is_saved_checked_and_defaults_to_living_there()
    {
        var c = _app.Browser();
        await AppFactory.SignUpAsync(c, $"purpose-{Guid.NewGuid():N}@example.com", "Nuria");
        Assert.Equal("live", (await c.GetFromJsonAsync<JsonElement>("/api/profile")).GetProperty("purpose").GetString());
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", Profile("Nuria", "let"))).StatusCode);
        Assert.Equal("let", (await c.GetFromJsonAsync<JsonElement>("/api/profile")).GetProperty("purpose").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/profile", Profile("Nuria", "holiday"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/profile", Profile("Nuria", null))).StatusCode);
    }

    [Fact]
    public async Task People_see_only_people_with_the_same_purpose()
    {
        var (c, _) = await Person("Lola", "let");
        var m = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        var shown = m.GetProperty("compatible").EnumerateArray().ToList();
        Assert.NotEmpty(shown); // made-up people buying to let
        Assert.All(shown, x => Assert.Equal("let", x.GetProperty("person").GetProperty("purpose").GetString()));
        Assert.All(shown, x => Assert.False(x.GetProperty("score").GetProperty("parts").TryGetProperty("lifestyle", out _)));
        var hidden = m.GetProperty("hidden").EnumerateArray().ToList();
        Assert.Contains(hidden, x => x.GetProperty("reasons")[0].GetProperty("key").GetString() == "purposeDiff");
        Assert.All(hidden.Where(x => x.GetProperty("person").GetProperty("purpose").GetString() == "live"),
            x => Assert.Equal("purposeDiff", x.GetProperty("reasons")[0].GetProperty("key").GetString()));

        // Switching to live in it swaps the lists round.
        await c.PutAsJsonAsync("/api/profile", Profile("Lola", "live"));
        var m2 = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        Assert.All(m2.GetProperty("compatible").EnumerateArray(), x => Assert.Equal("live", x.GetProperty("person").GetProperty("purpose").GetString()));
    }

    [Fact]
    public async Task A_letting_group_gets_a_letting_brief_and_breaks_if_someone_changes_purpose()
    {
        var (a, aId) = await Person("Iker", "let");
        var (b, bId) = await Person("Jone", "let");
        await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" });
        await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "like" });
        var r = await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var home = await r.Content.ReadFromJsonAsync<JsonElement>();
        var gid = home.GetProperty("id").GetString();
        Assert.Equal("let", home.GetProperty("purpose").GetString());
        Assert.Equal(0, home.GetProperty("rooms").GetArrayLength());
        Assert.Equal(0, home.GetProperty("tips").GetArrayLength());
        Assert.Equal("agLetKind", home.GetProperty("topics")[0].GetProperty("key").GetString());
        Assert.Equal(2, home.GetProperty("brief").GetProperty("beds").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/api/groups/{gid}/homes")).StatusCode);

        await b.PutAsJsonAsync("/api/profile", Profile("Jone", "live"));
        var after = await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}");
        Assert.False(after.GetProperty("check").GetProperty("ok").GetBoolean());
    }
}
