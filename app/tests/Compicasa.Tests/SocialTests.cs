using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>Session 19: notifications (B-037), messages, "liked you", block and report (B-036), and the group lifecycle (B-038).</summary>
public class SocialTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public SocialTests(AppFactory app) => _app = app;

    static int _n;
    static string NewEmail() => $"social{Interlocked.Increment(ref _n)}.{Guid.NewGuid().ToString("N")[..6]}@example.com";

    /// <summary>A signed-up, onboarded real person with the "room" profile, so real people match each other.</summary>
    static async Task<(HttpClient C, string Id)> PersonAsync(AppFactory app, string name)
    {
        var c = app.Browser();
        var id = await AppFactory.SignUpAsync(c, NewEmail(), name);
        await c.PutAsJsonAsync("/api/profile", ApiTests.RoomProfile(name));
        return (c, id);
    }
    Task<(HttpClient C, string Id)> PersonAsync(string name) => PersonAsync(_app, name);

    static async Task Like(HttpClient c, string id) =>
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/reactions/{id}", new { kind = "like" })).StatusCode);

    static async Task<JsonElement> Count(HttpClient c) => await c.GetFromJsonAsync<JsonElement>("/api/inbox/count");
    static async Task<string[]> Kinds(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/inbox")).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("kind").GetString()!).ToArray();
    static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task A_like_tells_the_other_person_who_sees_it_first_and_a_like_back_tells_the_first_of_the_match()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        await Like(a, bId);
        Assert.Equal(1, (await Count(b)).GetProperty("alerts").GetInt32());
        Assert.Equal(["like"], await Kinds(b));
        // In Bea's list, Ana is marked as having liked her.
        var m = await b.GetFromJsonAsync<JsonElement>("/api/matches");
        var ana = m.GetProperty("compatible").EnumerateArray().Single(x => x.GetProperty("person").GetProperty("id").GetString() == aId);
        Assert.True(ana.GetProperty("likedMe").GetBoolean());

        // Taking the like back removes the unread notice.
        await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "none" });
        Assert.Equal(0, (await Count(b)).GetProperty("alerts").GetInt32());
        await Like(a, bId);

        await Like(b, aId);
        // Newest first; Bea finished signing up after Ana, so Ana was also told someone compatible had joined (D-059).
        Assert.Equal(["match", "nearby"], await Kinds(a));
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsync("/api/inbox/read", null)).StatusCode);
        Assert.Equal(0, (await Count(a)).GetProperty("alerts").GetInt32());
    }

    [Fact]
    public async Task Mutual_matches_can_message_after_agreeing_to_the_code_of_conduct_and_nobody_else_can()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        var (c, _) = await PersonAsync("Cris");
        await Like(a, bId);
        // Not mutual yet: no conversation.
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/chats/person/{bId}")).StatusCode);
        await Like(b, aId);

        var first = await a.PostAsJsonAsync($"/api/chats/person/{bId}", new { body = "Hola Bea" });
        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        Assert.Equal("conductNeeded", (await Json(first)).GetProperty("error").GetString());
        Assert.True((await Json(await a.PostAsync("/api/me/conduct", null))).GetProperty("conduct").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync($"/api/chats/person/{bId}", new { body = "Hola Bea" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync($"/api/chats/person/{bId}", new { body = "  " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync($"/api/chats/person/{bId}", new { body = new string('x', 1001) })).StatusCode);

        Assert.Equal(1, (await Count(b)).GetProperty("messages").GetInt32());
        var chats = await b.GetFromJsonAsync<JsonElement>("/api/chats");
        var withAna = chats.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetString() == aId);
        Assert.Equal(1, withAna.GetProperty("unread").GetInt32());
        var thread = await b.GetFromJsonAsync<JsonElement>($"/api/chats/person/{aId}");
        Assert.Equal("Hola Bea", thread.GetProperty("messages")[0].GetProperty("body").GetString());
        // Reading it clears the unread count.
        Assert.Equal(0, (await Count(b)).GetProperty("messages").GetInt32());
        // Someone outside the pair can't read or write it.
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/chats/person/{aId}")).StatusCode);
    }

    [Fact]
    public async Task Made_up_people_reply_with_a_scripted_line()
    {
        var (c, _) = await PersonAsync("Rosa");
        await c.PostAsync("/api/me/conduct", null);
        var m = await c.GetFromJsonAsync<JsonElement>("/api/matches");
        foreach (var x in m.GetProperty("compatible").EnumerateArray())
            await Like(c, x.GetProperty("person").GetProperty("id").GetString()!);
        var chats = await c.GetFromJsonAsync<JsonElement>("/api/chats");
        var demo = chats.GetProperty("items").EnumerateArray().First(x => x.GetProperty("kind").GetString() == "person").GetProperty("id").GetString();
        var t = await Json(await c.PostAsJsonAsync($"/api/chats/person/{demo}", new { body = "Hola" }));
        var msgs = t.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(2, msgs.Count);
        Assert.Equal(demo, msgs[1].GetProperty("from").GetString());
        Assert.Equal("pHello", msgs[1].GetProperty("script").GetString());
        t = await Json(await c.PostAsJsonAsync($"/api/chats/person/{demo}", new { body = "¿Y la casa?" }));
        Assert.Equal("pBrief", t.GetProperty("messages")[3].GetProperty("script").GetString());
    }

    [Fact]
    public async Task Invitations_notify_a_decline_frees_a_place_and_a_member_fills_it_with_their_own_match()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        var (c, cId) = await PersonAsync("Cris");
        var (d, dId) = await PersonAsync("Dani");
        foreach (var (x, xId, y, yId) in new[] { (a, aId, b, bId), (a, aId, c, cId), (b, bId, d, dId) })
        { await Like(x, yId); await Like(y, xId); }

        var g = await Json(await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId, cId } }));
        var gid = g.GetProperty("id").GetString();
        Assert.Contains("invite", await Kinds(b));
        Assert.Equal(1, (await Count(c)).GetProperty("invites").GetInt32());

        // Bea says yes; Cris says no. The group carries on with Ana and Bea, formed, with one free place.
        await b.PostAsJsonAsync($"/api/groups/{gid}/respond", new { accept = true });
        var after = await Json(await c.PostAsJsonAsync($"/api/groups/{gid}/respond", new { accept = false }));
        Assert.Contains("declined", await Kinds(a));
        Assert.Contains("formed", await Kinds(b));
        var detail = await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}");
        Assert.Equal("formed", detail.GetProperty("state").GetString());
        Assert.Equal(1, detail.GetProperty("life").GetProperty("free").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/groups/{gid}")).StatusCode);

        // Ana can't invite Dani (not her match); Bea can, and Ana hears about it. Cris, who declined, can't come back.
        var no = await a.PostAsJsonAsync($"/api/groups/{gid}/invite", new { id = dId });
        Assert.Equal((HttpStatusCode)422, no.StatusCode);
        Assert.Equal((HttpStatusCode)422, (await a.PostAsJsonAsync($"/api/groups/{gid}/invite", new { id = cId })).StatusCode);
        var filled = await Json(await b.PostAsJsonAsync($"/api/groups/{gid}/invite", new { id = dId }));
        Assert.Equal("pending", filled.GetProperty("state").GetString());
        Assert.Contains("invited", await Kinds(a));
        Assert.Contains("invite", await Kinds(d));

        // Ana proposed the group, so she may withdraw Bea's invitation; Dani hears it and loses access.
        var w = await Json(await a.PostAsJsonAsync($"/api/groups/{gid}/withdraw", new { id = dId }));
        Assert.Equal("formed", w.GetProperty("state").GetString());
        Assert.Contains("withdrawn", await Kinds(d));
        Assert.DoesNotContain("invite", await Kinds(d));
        Assert.Equal(HttpStatusCode.NotFound, (await d.GetAsync($"/api/groups/{gid}")).StatusCode);
    }

    [Fact]
    public async Task Invitations_expire_after_14_days()
    {
        using var app = new AppFactory();
        var (a, aId) = await PersonAsync(app, "Ana");
        var (b, bId) = await PersonAsync(app, "Bea");
        await Like(a, bId); await Like(b, aId);
        var gid = (await Json(await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } }))).GetProperty("id").GetString();
        var mine = await b.GetFromJsonAsync<JsonElement>("/api/groups");
        Assert.Equal(app.Clock.GetUtcNow().AddDays(14), mine[0].GetProperty("life").GetProperty("expiresAt").GetDateTimeOffset());

        // Move the invitation 14 days and a minute into the past (moving the clock would expire the sign-in too).
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var row = db.GroupMembers.Single(m => m.AccountId == bId);
            row.InvitedAt = app.Clock.GetUtcNow().AddDays(-14).AddMinutes(-1);
            db.SaveChanges();
        }
        var late = await b.PostAsJsonAsync($"/api/groups/{gid}/respond", new { accept = true });
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Equal("expired", (await Json(late)).GetProperty("error").GetString());
        Assert.Equal(0, (await b.GetFromJsonAsync<JsonElement>("/api/groups")).GetArrayLength());
        Assert.Contains("expired", await Kinds(a));
        var g = await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}");
        Assert.Equal("broken", g.GetProperty("state").GetString());
        Assert.True(g.GetProperty("life").GetProperty("canInvite").GetBoolean());
    }

    [Fact]
    public async Task Blocking_hides_both_ways_leaves_shared_groups_and_a_report_is_kept()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        await Like(a, bId); await Like(b, aId);
        var gid = (await Json(await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } }))).GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync("/api/reports", new { about = aId, reason = "harassment", note = "test", block = true })).StatusCode);
        Assert.Equal((HttpStatusCode)400, (await b.PostAsJsonAsync("/api/reports", new { about = aId, reason = "nope" })).StatusCode);
        static bool Sees(JsonElement m, string id) => m.GetProperty("compatible").EnumerateArray().Any(x => x.GetProperty("person").GetProperty("id").GetString() == id);
        Assert.False(Sees(await a.GetFromJsonAsync<JsonElement>("/api/matches"), bId));
        Assert.False(Sees(await b.GetFromJsonAsync<JsonElement>("/api/matches"), aId));
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/chats/person/{bId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/groups/{gid}")).StatusCode);
        var blocks = await b.GetFromJsonAsync<JsonElement>("/api/blocks");
        Assert.Equal(aId, blocks[0].GetProperty("id").GetString());

        using (var scope = _app.Services.CreateScope())
            Assert.Single(scope.ServiceProvider.GetRequiredService<AppDb>().Reports.Where(r => r.AboutId == aId && r.Reason == "harassment"));

        Assert.Equal(HttpStatusCode.NoContent, (await b.DeleteAsync($"/api/blocks/{aId}")).StatusCode);
        Assert.True(Sees(await b.GetFromJsonAsync<JsonElement>("/api/matches"), aId));
    }

    [Fact]
    public async Task Someone_blocked_cant_like_or_pass_and_unread_notices_between_them_go()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        await Like(a, bId);
        Assert.Equal(1, (await Count(b)).GetProperty("alerts").GetInt32());

        // Two blocks of the same person at once (Session 32): both succeed.
        var blocks = await Task.WhenAll(b.PutAsync($"/api/blocks/{aId}", null), b.PutAsync($"/api/blocks/{aId}", null));
        Assert.All(blocks, r => Assert.Equal(HttpStatusCode.NoContent, r.StatusCode));
        Assert.Equal(0, (await Count(b)).GetProperty("alerts").GetInt32());

        // Neither side can react to the other now, so no new notice can reach the blocker.
        Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "none" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "pass" })).StatusCode);
        Assert.Equal(0, (await Count(b)).GetProperty("alerts").GetInt32());
    }

    [Fact]
    public async Task The_conversation_list_counts_unread_messages_per_conversation()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        await Like(a, bId); await Like(b, aId);
        await b.PostAsync("/api/me/conduct", null);
        await b.PostAsJsonAsync($"/api/chats/person/{aId}", new { body = "Hola" });
        await b.PostAsJsonAsync($"/api/chats/person/{aId}", new { body = "¿Qué tal?" });
        static int Unread(JsonElement chats, string id) =>
            chats.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetString() == id).GetProperty("unread").GetInt32();
        Assert.Equal(2, Unread(await a.GetFromJsonAsync<JsonElement>("/api/chats"), bId));
        Assert.Equal(0, Unread(await b.GetFromJsonAsync<JsonElement>("/api/chats"), aId)); // their own messages don't count
        await a.GetAsync($"/api/chats/person/{bId}");
        Assert.Equal(0, Unread(await a.GetFromJsonAsync<JsonElement>("/api/chats"), bId));
        await b.PostAsJsonAsync($"/api/chats/person/{aId}", new { body = "¿Sigues ahí?" });
        Assert.Equal(1, Unread(await a.GetFromJsonAsync<JsonElement>("/api/chats"), bId));
    }

    [Fact]
    public async Task Deleting_an_account_removes_its_messages_and_notifications()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        await Like(a, bId); await Like(b, aId);
        await a.PostAsync("/api/me/conduct", null);
        await a.PostAsJsonAsync($"/api/chats/person/{bId}", new { body = "Hola" });
        await b.PostAsync("/api/me/conduct", null);
        await b.PostAsJsonAsync($"/api/chats/person/{aId}", new { body = "Hola, Ana" });
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync("/api/me")).StatusCode);
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        Assert.False(db.Messages.Any(m => m.FromId == aId));
        // The whole conversation goes, the other person's side and their read mark included.
        var thread = Threads.Pair(aId, bId);
        Assert.False(db.Messages.Any(m => m.Thread == thread));
        Assert.False(db.ThreadReads.Any(r => r.Thread == thread));
        Assert.False(db.Notifications.Any(n => n.AccountId == aId || n.ActorId == aId));
        Assert.Equal(0, (await Count(b)).GetProperty("messages").GetInt32());
    }

    [Fact]
    public async Task Reading_a_conversation_several_times_at_once_never_fails()
    {
        var (a, aId) = await PersonAsync("Ana");
        var (b, bId) = await PersonAsync("Bea");
        await Like(a, bId); await Like(b, aId);
        await b.PostAsync("/api/me/conduct", null);
        await b.PostAsJsonAsync($"/api/chats/person/{aId}", new { body = "Hola" });
        // B-057: the first reads race to insert the same read mark; none may fail.
        var replies = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => a.GetAsync($"/api/chats/person/{bId}")));
        Assert.All(replies, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(0, (await Count(a)).GetProperty("messages").GetInt32());
    }
}
