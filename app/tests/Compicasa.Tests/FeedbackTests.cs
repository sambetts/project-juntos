using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>In-app feedback (B-081, D-052): signed-in people only, read on the admins' inbox. Made-up data only.</summary>
public class FeedbackTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public FeedbackTests(AppFactory app) => _app = app;

    async Task<List<ContactMessage>> StoredAsync(string marker)
    {
        using var scope = _app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDb>().ContactMessages.Where(m => m.Body.Contains(marker)).ToListAsync();
    }

    static async Task<string?> ErrorAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();

    [Theory]
    [InlineData("#home", "home")]
    [InlineData("#people-review", "people-review")]
    [InlineData("#person-p07", "person")]
    [InlineData("#chat-g-1234", "chat")]
    [InlineData("#group-0b5c0e4e-8f3a-4d2b-9a8e-1c2d3e4f5a6b-homes", "group-homes")]
    [InlineData("#group-0b5c0e4e-8f3a-4d2b-9a8e-1c2d3e4f5a6b", "group")]
    [InlineData("#houses", "houses")]
    [InlineData("#contact", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void The_page_keeps_only_the_kind_of_screen(string? hash, string? expected) => Assert.Equal(expected, Feedback.PageShape(hash));

    [Fact]
    public async Task Signed_out_people_cannot_send_feedback()
    {
        var r = await _app.Browser().PostAsJsonAsync("/api/feedback", new { kind = "idea", body = "Una idea estupenda" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Feedback_is_stored_with_the_account_and_the_minimum()
    {
        var c = _app.Browser();
        var id = await AppFactory.SignUpAsync(c, $"fb-{Guid.NewGuid():N}@example.com", "Nuria");
        var marker = Guid.NewGuid().ToString("N");
        var r = await c.PostAsJsonAsync("/api/feedback", new { kind = "wrong", body = $" El mapa no carga {marker} ", rating = 3, page = "#group-0b5c0e4e-8f3a-4d2b-9a8e-1c2d3e4f5a6b-homes", lang = "en" });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var m = Assert.Single(await StoredAsync(marker));
        Assert.Equal(id, m.AccountId);
        Assert.Equal("wrong", m.Topic);
        Assert.Equal(3, m.Rating);
        Assert.Equal("group-homes", m.Page);
        Assert.Equal("", m.Email); // not copied: the account links it
        Assert.Null(m.Name);
        Assert.Equal("en", m.Lang);
        Assert.StartsWith("El mapa", m.Body);
    }

    [Fact]
    public async Task Bad_feedback_is_refused()
    {
        var c = _app.Browser();
        await AppFactory.SignUpAsync(c, $"fb-b-{Guid.NewGuid():N}@example.com", "Pau");
        Assert.Equal("badKind", await ErrorAsync(await c.PostAsJsonAsync("/api/feedback", new { kind = "question", body = "Vale la pena" })));
        Assert.Equal("badRating", await ErrorAsync(await c.PostAsJsonAsync("/api/feedback", new { kind = "like", body = "Me gusta", rating = 6 })));
        Assert.Equal("badRating", await ErrorAsync(await c.PostAsJsonAsync("/api/feedback", new { kind = "like", body = "Me gusta", rating = 0 })));
        Assert.Equal("badFeedbackBody", await ErrorAsync(await c.PostAsJsonAsync("/api/feedback", new { kind = "idea", body = " a " })));
        Assert.Equal("badFeedbackBody", await ErrorAsync(await c.PostAsJsonAsync("/api/feedback", new { kind = "idea", body = new string('x', ContactMessage.BodyMax + 1) })));
        // Feedback kinds are not contact topics: the open form still refuses them.
        Assert.Equal("badTopic", await ErrorAsync(await c.PostAsJsonAsync("/api/contact", new { topic = "idea", email = "a@example.com", body = "Un mensaje suficiente" })));
    }

    [Fact]
    public async Task Admins_see_feedback_with_the_senders_email_counts_and_average()
    {
        var c = _app.Browser();
        var email = $"fb-s-{Guid.NewGuid():N}@example.com";
        await AppFactory.SignUpAsync(c, email, "Marta");
        var marker = Guid.NewGuid().ToString("N");
        await c.PostAsJsonAsync("/api/feedback", new { kind = "like", body = $"Me encanta {marker}", rating = 5 });
        await c.PostAsJsonAsync("/api/feedback", new { kind = "idea", body = $"Una idea {marker}" });

        var user = await c.GetFromJsonAsync<JsonElement>("/api/admin/contact");
        Assert.False(user.GetProperty("allowed").GetBoolean());

        var admin = _app.Browser();
        _app.Services.GetRequiredService<IConfiguration>()["Admin:AccountIds"] = await AppFactory.SignUpAsync(admin, $"fb-a-{Guid.NewGuid():N}@example.com", "Admin");
        var d = await admin.GetFromJsonAsync<JsonElement>("/api/admin/contact");
        Assert.True(d.GetProperty("allowed").GetBoolean());
        var mine = d.GetProperty("messages").EnumerateArray().Where(m => m.GetProperty("body").GetString()!.Contains(marker)).ToList();
        Assert.Equal(2, mine.Count);
        Assert.All(mine, m => { Assert.Equal(email, m.GetProperty("email").GetString()); Assert.True(m.GetProperty("feedback").GetBoolean()); });
        Assert.Equal(5, mine.Single(m => m.GetProperty("topic").GetString() == "like").GetProperty("rating").GetInt32());
        Assert.True(d.GetProperty("counts").GetProperty("like").GetProperty("open").GetInt32() >= 1);
        Assert.True(d.GetProperty("ratings").GetProperty("count").GetInt32() >= 1);
    }
}
