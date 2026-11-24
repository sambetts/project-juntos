using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Core;

namespace Compicasa.Tests;

/// <summary>B-042 (release checklist item 4): household type never filters a real person, either way (AGENTS.md section 4, D-009).
/// Between two made-up people the prototype's rule still applies, so the demo and the parity tests are unchanged. Made-up data only.</summary>
public class HouseholdTests
{
    static Profile P(string id, string household, string[] open, bool real = false) => new()
    {
        Id = id, Name = id, Household = household, Open = [.. open], Real = real,
        Adults = household is "couple" or "family" ? 2 : 1, Kids = household is "family" or "singleParent" ? 1 : 0,
        Areas = ["centre"], Types = ["flat"],
    };

    [Fact]
    public void Two_made_up_people_keep_the_household_rule()
    {
        var single = P("s", "single", ["single"]);
        var family = P("f", "family", ["single", "couple", "family", "singleParent"]);
        Assert.Contains(Matching.Hard(single, family).Mine, m => m.Key == "notOpen");
        Assert.Contains("household", Matching.Hard(family, single).Theirs);
    }

    [Fact]
    public void A_real_person_is_never_filtered_by_household_type_either_way()
    {
        // The real person's own stored preference is ignored ...
        var realSingle = P("r", "single", ["single"], real: true);
        var madeUpFamily = P("f", "family", ["single", "couple", "family", "singleParent"]);
        Assert.True(Matching.Hard(realSingle, madeUpFamily).Ok);
        Assert.True(Matching.Hard(madeUpFamily, realSingle).Ok);
        // ... and so is a made-up person's preference about the real person.
        var realFamily = P("q", "family", ["single", "couple", "family", "singleParent"], real: true);
        var madeUpPicky = P("m", "single", ["single"]);
        Assert.True(Matching.Hard(realFamily, madeUpPicky).Ok);
        Assert.True(Matching.Hard(madeUpPicky, realFamily).Ok);
        Assert.Equal(Matching.Accepts(madeUpPicky, realFamily), Matching.Accepts(madeUpPicky, P("q2", "single", ["single"], real: true)));
        // Two real people: no household rule at all.
        Assert.True(Matching.Hard(realSingle, realFamily).Ok);
    }

    [Fact]
    public void The_real_flag_survives_a_clone_and_is_never_stored()
    {
        var p = P("r", "single", ["single"], real: true);
        Assert.True(p.Clone().Real);
        Assert.DoesNotContain("real", JsonSerializer.Serialize(p, Seeds.Json), StringComparison.OrdinalIgnoreCase);
    }
}

public class HouseholdApiTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public HouseholdApiTests(AppFactory app) => _app = app;

    [Fact]
    public async Task Saving_a_profile_ignores_who_you_would_share_with_and_no_one_is_hidden_for_it()
    {
        var c = _app.Browser();
        await AppFactory.SignUpAsync(c, $"household-{Guid.NewGuid():N}@example.com", "Marta");
        var profile = new
        {
            name = "Marta", household = "family", kids = 2, bedrooms = 2, privateBath = false, cars = 0, pets = Array.Empty<string>(), acceptsPets = true,
            smoking = "no", acceptsSmoking = "outside", sleep = 3, tidy = 3, social = 3, guests = 3, meals = 3, wfh = false, horizon = "5-9",
            areas = new[] { "centre", "city", "north", "south", "east" }, types = new[] { "flat", "house" },
            needs = new Dictionary<string, string>(), open = new[] { "family" }, maxHouseholds = 4, purpose = "live",
        };
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", profile)).StatusCode);
        var saved = await c.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal(Catalogue.Households, saved.GetProperty("open").EnumerateArray().Select(x => x.GetString()!).ToArray());
        // Leaving the field out (the app no longer sends it) is fine too.
        var withoutOpen = JsonSerializer.SerializeToElement(profile).EnumerateObject().Where(x => x.Name != "open").ToDictionary(x => x.Name, x => x.Value);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/profile", withoutOpen)).StatusCode);

        var m = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        foreach (var h in m.GetProperty("hidden").EnumerateArray())
            Assert.DoesNotContain(h.GetProperty("reasons").EnumerateArray(), r => r.GetProperty("key").GetString() == "notOpen");
    }
}
