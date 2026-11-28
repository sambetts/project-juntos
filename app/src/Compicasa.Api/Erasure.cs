using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// A sign-in record at Microsoft Entra External ID still to be removed after its account was deleted (B-039).
/// Only the record's object id is kept here, never the email, and the row goes as soon as the record is gone.
/// </summary>
public sealed class SignInErasure
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = "";
    public DateTimeOffset RequestedAt { get; set; }
    public int Tries { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Removes External ID sign-in records through Microsoft Graph (DELETE /users/{id}), as the app registration named by
/// <c>Auth:Erase:ClientId</c> in the External ID tenant (<c>Auth:External:TenantId</c>), with the Graph application
/// permission User.ReadWrite.All. Set up by <c>app/infra/external-id.ps1 -EraseApp</c>. Without those settings
/// (local runs, tests) the requests wait in <see cref="SignInErasure"/> and admins see how many on <c>#reports</c>.
/// </summary>
public sealed class SignInEraser(IConfiguration cfg, IHttpClientFactory http, ILogger<SignInEraser> log)
{
    string? _token;
    DateTimeOffset _tokenUntil;

    public bool Configured =>
        !string.IsNullOrEmpty(cfg["Auth:External:TenantId"]) && !string.IsNullOrEmpty(cfg["Auth:Erase:ClientId"]) && !string.IsNullOrEmpty(cfg["Auth:Erase:ClientSecret"]);

    /// <summary>An app-only Graph token by the client credentials grant, kept until shortly before it expires.</summary>
    async Task<string> TokenAsync(CancellationToken ct)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _tokenUntil) return _token;
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = cfg["Auth:Erase:ClientId"]!, ["client_secret"] = cfg["Auth:Erase:ClientSecret"]!,
            ["scope"] = "https://graph.microsoft.com/.default", ["grant_type"] = "client_credentials",
        });
        using var res = await http.CreateClient("graph").PostAsync($"https://login.microsoftonline.com/{Uri.EscapeDataString(cfg["Auth:External:TenantId"]!)}/oauth2/v2.0/token", form, ct);
        if (!res.IsSuccessStatusCode) throw new HttpRequestException($"token {(int)res.StatusCode}");
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        _token = doc.RootElement.GetProperty("access_token").GetString()!;
        _tokenUntil = DateTimeOffset.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32() - 120);
        return _token;
    }

    /// <summary>Removes one sign-in record. True when it's gone (or was never there).</summary>
    public async Task<(bool Done, string? Error)> EraseAsync(string externalId, CancellationToken ct)
    {
        if (!Configured) return (false, "notConfigured");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(externalId)}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(ct));
            using var res = await http.CreateClient("graph").SendAsync(req, ct);
            if (res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.NotFound) return (true, null);
            return (false, $"graph {(int)res.StatusCode}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Removing a sign-in record at External ID failed; will retry");
            return (false, ex.GetType().Name);
        }
    }

    /// <summary>Works through the waiting requests. Returns how many are still waiting.</summary>
    public async Task<int> RunAsync(AppDb db, CancellationToken ct)
    {
        var waiting = await db.SignInErasures.OrderBy(x => x.Id).Take(50).ToListAsync(ct);
        if (!Configured) return await db.SignInErasures.CountAsync(ct);
        foreach (var w in waiting)
        {
            var (done, error) = await EraseAsync(w.ExternalId, ct);
            if (done) db.SignInErasures.Remove(w);
            else { w.Tries++; w.LastError = error is { Length: > 200 } ? error[..200] : error; }
        }
        await db.SaveChangesAsync(ct);
        return await db.SignInErasures.CountAsync(ct);
    }
}

/// <summary>Retries waiting sign-in record removals every hour (B-039), and deletes old reports about deleted accounts (B-052).</summary>
public sealed class SignInErasureWorker(IServiceScopeFactory scopes, ILogger<SignInErasureWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDb>();
                var left = await scope.ServiceProvider.GetRequiredService<SignInEraser>().RunAsync(db, stop);
                // Retention for reports about deleted accounts (B-052) rides on the same hourly run.
                await Moderation.PurgeAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow(), stop);
                if (left > 0) log.LogWarning("{Count} External ID sign-in records still waiting to be removed", left);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Sign-in record removal run failed"); }
            try { await Task.Delay(TimeSpan.FromHours(1), stop); } catch (OperationCanceledException) { return; }
        }
    }
}
