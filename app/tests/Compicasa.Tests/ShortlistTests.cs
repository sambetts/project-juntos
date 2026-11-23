using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>Session 22: house voting for a group (B-056).</summary>
public class ShortlistTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public ShortlistTests(AppFactory app) => _app = app;

    static int _n;
    static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    async Task<(HttpClient C, string Id)> Person(string name)
    {
        var c = _app.Browser();
        var id = await AppFactory.SignUpAsync(c, $"vote{Interlocked.Increment(ref _n)}-{Guid.NewGuid():N}@example.com", name);
        await c.PutAsJsonAsync("/api/profile", ApiTests.RoomProfile(name));
        return (c, id);
    }

    /// <summary>Ana and Bea, mutual matches, in a formed group of two.</summary>
    async Task<(HttpClient A, string AId, HttpClient B, string BId, string Gid)> PairGroup()
    {
        var (a, aId) = await Person("Ana");
        var (b, bId) = await Person("Bea");
        await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" });
        await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "like" });
        var gid = (await Json(await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } }))).GetProperty("id").GetString()!;
        await b.PostAsJsonAsync($"/api/groups/{gid}/respond", new { accept = true });
        return (a, aId, b, bId, gid);
    }

    static async Task<string> FirstHome(HttpClient c, string gid) =>
        (await c.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/homes")).GetProperty("homes")[0].GetProperty("id").GetString()!;

    static JsonElement Item(JsonElement list) => list.GetProperty("items")[0];
    static Task<HttpResponseMessage> Vote(HttpClient c, string gid, long cid, string choice, string? reason = null) =>
        c.PutAsJsonAsync($"/api/groups/{gid}/shortlist/{cid}/vote", new { choice, reason });

    static async Task<string[]> Kinds(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/inbox")).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("kind").GetString()!).ToArray();

    [Fact]
    public async Task Votes_stay_hidden_until_everyone_has_voted_and_three_yes_rounds_agree_a_home()
    {
        var (a, _, b, bId, gid) = await PairGroup();
        var homeId = await FirstHome(a, gid);
        var added = await a.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var item = Item(await Json(added));
        var cid = item.GetProperty("id").GetInt64();
        Assert.Equal("view", item.GetProperty("stageName").GetString());
        Assert.Contains("shortlisted", await Kinds(b));
        // The same home twice, or a home that isn't in the index, is refused.
        Assert.Equal(HttpStatusCode.Conflict, (await b.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId = "nope:1" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Vote(a, gid, cid, "perhaps")).StatusCode);

        // Ana says maybe: Bea sees that Ana has voted, not how.
        await Vote(a, gid, cid, "maybe", "size");
        var seen = Item(await b.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/shortlist"));
        Assert.False(seen.GetProperty("revealed").GetBoolean());
        var anaVote = seen.GetProperty("votes").EnumerateArray().First(v => v.GetProperty("id").GetString() != bId);
        Assert.True(anaVote.GetProperty("voted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, anaVote.GetProperty("choice").ValueKind);

        // Bea votes yes: everyone has voted, so both choices show; still on "view", since Ana said maybe.
        var both = Item(await Json(await Vote(b, gid, cid, "yes")));
        Assert.True(both.GetProperty("revealed").GetBoolean());
        Assert.Equal("view", both.GetProperty("stageName").GetString());
        Assert.Contains(both.GetProperty("votes").EnumerateArray(), v => v.GetProperty("choice").GetString() == "maybe" && v.GetProperty("reason").GetString() == "size");

        // Ana changes her mind: "view" is agreed and "offer" opens, with fresh, hidden votes.
        var next = Item(await Json(await Vote(a, gid, cid, "yes")));
        Assert.Equal("offer", next.GetProperty("stageName").GetString());
        Assert.False(next.GetProperty("revealed").GetBoolean());
        Assert.Contains("agreeView", await Kinds(b));

        await Vote(a, gid, cid, "yes"); await Vote(b, gid, cid, "yes");
        await Vote(a, gid, cid, "yes");
        var done = Item(await Json(await Vote(b, gid, cid, "yes")));
        Assert.Equal("agreed", done.GetProperty("stageName").GetString());
        Assert.Contains("agreed", await Kinds(a));
        Assert.Equal(HttpStatusCode.Conflict, (await Vote(a, gid, cid, "no", "price")).StatusCode);

        // Only people in the group see or change the shortlist.
        var (o, _) = await Person("Olga");
        Assert.Equal(HttpStatusCode.NotFound, (await o.GetAsync($"/api/groups/{gid}/shortlist")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Vote(o, gid, cid, "yes")).StatusCode);
    }

    [Fact]
    public async Task A_price_change_sends_the_group_back_to_vote_again()
    {
        var (a, _, b, _, gid) = await PairGroup();
        var homeId = await FirstHome(a, gid);
        var cid = Item(await Json(await a.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId }))).GetProperty("id").GetInt64();
        await Vote(a, gid, cid, "yes");
        Assert.Equal("offer", Item(await Json(await Vote(b, gid, cid, "yes"))).GetProperty("stageName").GetString());

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var h = await db.Homes.SingleAsync(x => x.Id == homeId);
            h.Price = (h.Price ?? 200000) - 10000;
            await db.SaveChangesAsync();
        }
        var after = Item(await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/shortlist"));
        Assert.Equal("view", after.GetProperty("stageName").GetString());
        Assert.NotEqual(JsonValueKind.Null, after.GetProperty("repriced").ValueKind);
        Assert.All(after.GetProperty("votes").EnumerateArray(), v => Assert.False(v.GetProperty("voted").GetBoolean()));
    }

    [Fact]
    public async Task Only_whoever_added_a_home_or_the_proposer_can_take_it_off()
    {
        var (a, _, b, _, gid) = await PairGroup();
        var homes = (await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/homes")).GetProperty("homes");
        var byAna = Item(await Json(await a.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId = homes[0].GetProperty("id").GetString() }))).GetProperty("id").GetInt64();
        var list = await Json(await b.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId = homes[1].GetProperty("id").GetString() }));
        var byBea = list.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetInt64() != byAna).GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.Forbidden, (await b.DeleteAsync($"/api/groups/{gid}/shortlist/{byAna}")).StatusCode);
        // Ana proposed the group, so she may take off Bea's too.
        Assert.Equal(HttpStatusCode.OK, (await a.DeleteAsync($"/api/groups/{gid}/shortlist/{byBea}")).StatusCode);
        var left = await b.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/shortlist");
        Assert.Equal(byAna, Assert.Single(left.GetProperty("items").EnumerateArray()).GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Made_up_members_vote_at_once_by_how_well_the_home_fits()
    {
        Assert.Equal(("yes", (string?)null), Shortlist.DemoVote("demo-1", "demo:1", 90));
        Assert.Equal(Shortlist.DemoVote("demo-1", "demo:7", 50), Shortlist.DemoVote("demo-1", "demo:7", 50));
        Assert.NotEqual("yes", Shortlist.DemoVote("demo-1", "demo:7", 50).Choice);

        var (c, _) = await Person("Rosa");
        var m = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        foreach (var x in m.GetProperty("compatible").EnumerateArray())
            await c.PutAsJsonAsync($"/api/reactions/{x.GetProperty("person").GetProperty("id").GetString()}", new { kind = "like" });
        var s = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        var demo = s.GetProperty("compatible").EnumerateArray().First(x => x.GetProperty("state").GetString() == "mutual").GetProperty("person").GetProperty("id").GetString()!;
        var g = await Json(await c.PostAsJsonAsync("/api/groups", new { ids = new[] { demo } }));
        Assert.Equal("formed", g.GetProperty("state").GetString());
        var gid = g.GetProperty("id").GetString()!;
        var homeId = await FirstHome(c, gid);
        var item = Item(await Json(await c.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId })));
        var theirs = item.GetProperty("votes").EnumerateArray().Single(v => v.GetProperty("id").GetString() == demo);
        Assert.True(theirs.GetProperty("voted").GetBoolean());
    }

    [Fact]
    public async Task Deleting_an_account_removes_its_groups_shortlists_and_votes()
    {
        var (a, _, b, _, gid) = await PairGroup();
        var homeId = await FirstHome(a, gid);
        var cid = Item(await Json(await a.PostAsJsonAsync($"/api/groups/{gid}/shortlist", new { homeId }))).GetProperty("id").GetInt64();
        await Vote(b, gid, cid, "yes");
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync("/api/me")).StatusCode);
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        Assert.False(await db.Candidates.AnyAsync(x => x.Id == cid));
        Assert.False(await db.CandidateVotes.AnyAsync(x => x.CandidateId == cid));
    }
}
