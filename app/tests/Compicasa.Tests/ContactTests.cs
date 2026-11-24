using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compicasa.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>The contact form (B-078): anyone may write; only listed admins read. Made-up data only.</summary>
public class ContactTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public ContactTests(AppFactory app) => _app = app;

    async Task<List<ContactMessage>> StoredAsync(string marker)
    {
        using var scope = _app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDb>().ContactMessages.Where(m => m.Body.Contains(marker)).ToListAsync();
    }

    static async Task<string?> ErrorAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();

    [Fact]
    public async Task Anyone_can_write_without_an_account()
    {
        var marker = Guid.NewGuid().ToString("N");
        var r = await _app.Browser().PostAsJsonAsync("/api/contact", new { topic = "question", name = " Lola ", email = "Lola@Example.com", body = $"Hola, una duda {marker}", lang = "es" });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var m = Assert.Single(await StoredAsync(marker));
        Assert.Null(m.AccountId);
        Assert.Equal("Lola", m.Name);
        Assert.Equal("question", m.Topic);
        Assert.Null(m.DoneAt);
    }

    [Fact]
    public async Task A_signed_in_sender_is_linked_to_their_account()
    {
        var c = _app.Browser();
        var id = await AppFactory.SignUpAsync(c, $"ct-{Guid.NewGuid():N}@example.com", "Rosa");
        var marker = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/contact", new { topic = "privacy", email = "rosa@example.com", body = $"Quiero mis datos {marker}" })).StatusCode);
        Assert.Equal(id, Assert.Single(await StoredAsync(marker)).AccountId);
    }

    [Fact]
    public async Task Bad_input_is_refused_and_the_bot_trap_stores_nothing()
    {
        var c = _app.Browser();
        Assert.Equal("badEmail", await ErrorAsync(await c.PostAsJsonAsync("/api/contact", new { topic = "question", email = "no", body = "Un mensaje suficiente" })));
        Assert.Equal("badTopic", await ErrorAsync(await c.PostAsJsonAsync("/api/contact", new { topic = "sales", email = "a@example.com", body = "Un mensaje suficiente" })));
        Assert.Equal("badContactBody", await ErrorAsync(await c.PostAsJsonAsync("/api/contact", new { topic = "other", email = "a@example.com", body = "corto" })));
        Assert.Equal("badContactBody", await ErrorAsync(await c.PostAsJsonAsync("/api/contact", new { topic = "other", email = "a@example.com", body = new string('x', ContactMessage.BodyMax + 1) })));
        var marker = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/contact", new { topic = "other", email = "a@example.com", body = $"Spam {marker}", website = "http://spam.example" })).StatusCode);
        Assert.Empty(await StoredAsync(marker));
    }

    [Fact]
    public async Task Only_listed_admins_read_and_close_messages()
    {
        var marker = Guid.NewGuid().ToString("N");
        await _app.Browser().PostAsJsonAsync("/api/contact", new { topic = "problem", email = "b@example.com", body = $"No funciona {marker}" });
        var msg = Assert.Single(await StoredAsync(marker));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Browser().GetAsync("/api/admin/contact")).StatusCode);
        var user = _app.Browser();
        await AppFactory.SignUpAsync(user, $"ct-u-{Guid.NewGuid():N}@example.com", "Iker");
        Assert.False((await user.GetFromJsonAsync<JsonElement>("/api/admin/contact")).GetProperty("allowed").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await user.PostAsync($"/api/admin/contact/{msg.Id}/done", null)).StatusCode);

        var admin = _app.Browser();
        var adminId = await AppFactory.SignUpAsync(admin, $"ct-a-{Guid.NewGuid():N}@example.com", "Admin");
        _app.Services.GetRequiredService<IConfiguration>()["Admin:AccountIds"] = adminId;
        var list = await admin.GetFromJsonAsync<JsonElement>("/api/admin/contact");
        Assert.True(list.GetProperty("allowed").GetBoolean());
        Assert.Contains(list.GetProperty("messages").EnumerateArray(), m => m.GetProperty("body").GetString()!.Contains(marker));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/contact/{msg.Id}/done", null)).StatusCode);
        Assert.NotNull(Assert.Single(await StoredAsync(marker)).DoneAt);

        // Dealt-with messages go a year later (a fresh admin session, since the old one has expired by then).
        _app.Clock.Advance(TimeSpan.FromDays(ContactMessage.KeepDoneDays + 1));
        var later = _app.Browser();
        _app.Services.GetRequiredService<IConfiguration>()["Admin:AccountIds"] = await AppFactory.SignUpAsync(later, $"ct-l-{Guid.NewGuid():N}@example.com", "Admin");
        Assert.True((await later.GetFromJsonAsync<JsonElement>("/api/admin/contact")).GetProperty("allowed").GetBoolean());
        Assert.Empty(await StoredAsync(marker));
    }
}
