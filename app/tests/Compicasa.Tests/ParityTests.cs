using System.Text.Json;
using Compicasa.Core;

namespace Compicasa.Tests;

/// <summary>
/// The C# engine must score exactly like the prototype it was ported from. The fixture is made by
/// tests/parity/export-from-prototype.js from prototype/compicasa-prototype.html.
/// </summary>
public class ParityTests
{
    static readonly JsonElement Fx = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prototype-parity.json"))).RootElement;

    static Dictionary<string, Profile> People()
    {
        var d = Seeds.Profiles().ToDictionary(p => p.Id);
        foreach (var g in Fx.GetProperty("groups").EnumerateArray())
        {
            var u = g.GetProperty("user").Deserialize<Profile>(Seeds.Json)!;
            u.Id = "you-" + g.GetProperty("preset").GetString();
            d[u.Id] = u;
        }
        return d;
    }

    [Fact]
    public void Fourteen_made_up_profiles_load()
    {
        var s = Seeds.Profiles();
        Assert.Equal(14, s.Count);
        Assert.All(s, p => { Assert.NotNull(p.Bio); Assert.False(string.IsNullOrWhiteSpace(p.Bio!.Es)); Assert.False(string.IsNullOrWhiteSpace(p.Bio.En)); });
        Assert.Equal(14, s.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void Every_pair_scores_and_matches_like_the_prototype()
    {
        var people = People();
        var n = 0;
        foreach (var x in Fx.GetProperty("pairs").EnumerateArray())
        {
            var a = people[x.GetProperty("a").GetString()!]; var b = people[x.GetProperty("b").GetString()!];
            var label = $"{a.Id}->{b.Id}";
            var h = Matching.Hard(a, b); var s = Matching.Score(a, b); var ex = Matching.Explain(a, b);
            Assert.True(x.GetProperty("ok").GetBoolean() == h.Ok, label + " ok");
            Assert.True(x.GetProperty("mine").GetInt32() == h.Mine.Count, label + " mine");
            Assert.True(x.GetProperty("theirs").GetInt32() == h.Theirs.Count, label + " theirs");
            Assert.True(x.GetProperty("both").GetInt32() == h.Both.Count, label + " both");
            Assert.True(x.GetProperty("total").GetInt32() == s.Total, $"{label} total {s.Total}");
            foreach (var part in x.GetProperty("parts").EnumerateObject())
                Assert.True(Math.Abs(part.Value.GetDouble() - s.Parts[part.Name]) < 1e-12, $"{label} {part.Name}");
            Assert.True(x.GetProperty("accepts").GetBoolean() == Matching.Accepts(a, b), label + " accepts");
            Assert.True(x.GetProperty("clicks").GetInt32() == ex.Clicks.Count, label + " clicks");
            Assert.True(x.GetProperty("talks").GetInt32() == ex.Talks.Count, label + " talks");
            n++;
        }
        Assert.Equal(240, n);
    }

    [Fact]
    public void Group_suggestions_and_briefs_match_the_prototype()
    {
        var seeds = Seeds.Profiles();
        foreach (var g in Fx.GetProperty("groups").EnumerateArray())
        {
            var u = g.GetProperty("user").Deserialize<Profile>(Seeds.Json)!;
            // The prototype's user liked everyone; a seed is a mutual match when it would accept the user back.
            var mutual = seeds.Where(p => Matching.Accepts(p, u)).ToList();
            var sugg = Matching.BestGroups(u, mutual);
            var exp = g.GetProperty("suggestions").EnumerateArray().ToList();
            Assert.Equal(exp.Count, sugg.Count);
            for (var i = 0; i < exp.Count; i++)
            {
                Assert.Equal(exp[i].GetProperty("ids").EnumerateArray().Select(e => e.GetString()), sugg[i].Ids);
                Assert.Equal(exp[i].GetProperty("min").GetInt32(), sugg[i].Min);
                Assert.Equal(exp[i].GetProperty("avg").GetInt32(), sugg[i].Avg);
            }
            foreach (var b in g.GetProperty("briefs").EnumerateArray())
            {
                var all = new List<Profile> { u };
                all.AddRange(b.GetProperty("ids").EnumerateArray().Select(e => seeds.First(p => p.Id == e.GetString())));
                var hb = Matching.Brief(all);
                Assert.Equal(b.GetProperty("beds").GetInt32(), hb.Beds);
                Assert.Equal(b.GetProperty("baths").GetInt32(), hb.Baths);
                Assert.Equal(b.GetProperty("cars").GetInt32(), hb.Cars);
                Assert.Equal(b.GetProperty("m2").GetInt32(), hb.M2);
                Assert.Equal(b.GetProperty("m2min").GetInt32(), hb.M2Min);
                Assert.Equal(b.GetProperty("m2max").GetInt32(), hb.M2Max);
                Assert.Equal(b.GetProperty("areas").EnumerateArray().Select(e => e.GetString()), hb.Areas);
                Assert.Equal(b.GetProperty("types").EnumerateArray().Select(e => e.GetString()), hb.Types);
                Assert.Equal(b.GetProperty("feats").EnumerateArray().Select(e => e.GetString()), hb.Feats.Select(f => f.Id + ":" + f.Level));
                Assert.Equal(b.GetProperty("extras").GetInt32(), hb.Extras.Count);
                Assert.Equal(b.GetProperty("rooms").EnumerateArray().Select(e => e.GetInt32()), Matching.RoomPlan(all).Select(r => r.Pct));
                Assert.Equal(b.GetProperty("tips").GetInt32(), Matching.PlacementTips(all, u.Id).Count);
                Assert.Equal(b.GetProperty("topics").GetInt32(), Matching.AgreementTopics(all).Count);
                Assert.Equal(b.GetProperty("check").GetBoolean(), Matching.GroupCheck(all).Ok);
            }
        }
    }

    [Fact]
    public void Whole_group_checks_match_the_prototype()
    {
        var seeds = Seeds.Profiles().ToDictionary(p => p.Id);
        foreach (var c in Fx.GetProperty("checks").EnumerateArray())
        {
            var all = c.GetProperty("ids").EnumerateArray().Select(e => seeds[e.GetString()!]).ToList();
            var r = Matching.GroupCheck(all); var gs = Matching.GroupScore(all);
            var label = string.Join("+", all.Select(m => m.Id));
            Assert.True(c.GetProperty("ok").GetBoolean() == r.Ok, label);
            Assert.True(c.GetProperty("issues").GetInt32() == r.Issues.Count, label + " issues");
            Assert.Equal(c.GetProperty("min").GetInt32(), gs.Min);
            Assert.Equal(c.GetProperty("avg").GetInt32(), gs.Avg);
        }
    }

    [Fact]
    public void Household_type_and_accessibility_needs_never_change_the_score()
    {
        var seeds = Seeds.Profiles();
        foreach (var a in seeds)
            foreach (var b in seeds.Where(b => b != a))
            {
                var baseScore = Matching.Score(a, b).Total;
                var a2 = a.Clone(); a2.Household = a.Household == "single" ? "family" : "single";
                a2.Needs["lift"] = "must"; a2.Needs["stepfree"] = "must";
                var b2 = b.Clone(); b2.Needs.Remove("lift"); b2.Needs.Remove("stepfree");
                Assert.Equal(baseScore, Matching.Score(a2, b).Total);
                Assert.Equal(Matching.Score(a, b2).Total, Matching.Score(a2, b2).Total);
                Assert.Equal(Matching.Score(a, b).Total, Matching.Score(b, a).Total);
            }
    }
}
