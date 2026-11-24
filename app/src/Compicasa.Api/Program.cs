using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Compicasa.Api;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Database: SQLite on a developer's machine and in tests; Azure SQL in Azure, signed in with the app's
// managed identity (connection string "Authentication=Active Directory Default"), so no password exists.
var provider = cfg["Database:Provider"] ?? "Sqlite";
var conn = cfg.GetConnectionString("Db") ?? "Data Source=compicasa.db";
builder.Services.AddDbContext<AppDb>(o =>
{
    if (provider == "SqlServer" && cfg["Database:Credential"] == "AzureCli")
    {
        // For running the crawler from a developer's machine (D-035): sign in to Azure SQL with the Azure CLI's
        // account only, since the default credential chain can pick up another account on the machine.
        var tenant = cfg["Database:TenantId"];
        var sql = new Microsoft.Data.SqlClient.SqlConnection(conn)
        {
            AccessTokenCallback = async (_, ct) =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd.exe" : "az")
                { RedirectStandardOutput = true, UseShellExecute = false };
                if (OperatingSystem.IsWindows()) { psi.ArgumentList.Add("/c"); psi.ArgumentList.Add("az"); }
                foreach (var a in new[] { "account", "get-access-token", "--resource", "https://database.windows.net/", "-o", "json" }) psi.ArgumentList.Add(a);
                if (!string.IsNullOrEmpty(tenant)) { psi.ArgumentList.Add("--tenant"); psi.ArgumentList.Add(tenant); }
                using var p = System.Diagnostics.Process.Start(psi)!;
                var json = await p.StandardOutput.ReadToEndAsync(ct);
                await p.WaitForExitAsync(ct);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var r = doc.RootElement;
                return new Microsoft.Data.SqlClient.SqlAuthenticationToken(r.GetProperty("accessToken").GetString()!,
                    DateTimeOffset.FromUnixTimeSeconds(r.GetProperty("expires_on").GetInt64()));
            },
        };
        o.UseSqlServer(sql, contextOwnsConnection: true, s => s.EnableRetryOnFailure(6, TimeSpan.FromSeconds(10), null));
    }
    else if (provider == "SqlServer") o.UseSqlServer(conn, s => s.EnableRetryOnFailure(6, TimeSpan.FromSeconds(10), null));
    else o.UseSqlite(conn);
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<DemoData>();
if (cfg.GetValue("Demo:RefreshDaily", true)) builder.Services.AddHostedService<DemoRefreshWorker>();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

Accounts.AddAuthentication(builder);
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = cfg.GetValue("Auth:AttemptsPerMinute", 10), Window = TimeSpan.FromMinutes(1) }));
    // The contact form is open to anyone, so it gets a tighter limit per address (B-078).
    o.AddPolicy("contact", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = cfg.GetValue("Contact:PerHour", 5), Window = TimeSpan.FromHours(1) }));
});

// App Service sits behind a proxy: take the client address and scheme from it.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

if (!string.IsNullOrEmpty(cfg["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry().UseAzureMonitor();

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "no-referrer";
    h["X-Frame-Options"] = "DENY";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // Made-up data, closed PoC (D-027): keep it out of search engines.
    h["X-Robots-Tag"] = "noindex, nofollow";
    h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; connect-src 'self'; manifest-src 'self'; worker-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    // Changing requests must carry our header: a cross-site form can't add one, so this blocks CSRF.
    if (ctx.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method)
        && ctx.Request.Headers[Api.CsrfHeader] != "1")
    {
        ctx.Response.StatusCode = 403;
        await ctx.Response.WriteAsJsonAsync(new { error = "csrf" });
        return;
    }
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var name = ctx.File.Name;
        ctx.Context.Response.Headers.CacheControl = name is "index.html" or "sw.js" or "manifest.webmanifest" ? "no-cache" : "public, max-age=3600";
    },
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Text("ok"));
app.MapGet("/healthz/ready", async (AppDb db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Text("ready") : Results.StatusCode(503));
Accounts.Map(app);
Api.Map(app);
Social.Map(app);
Shortlist.Map(app);
Funnel.Map(app);
Contact.Map(app);
Browse.Map(app);
// What our crawler does and how to block it (D-035); named in its user agent.
app.MapGet("/bot", (IWebHostEnvironment env) => Results.File(Path.Combine(env.WebRootPath, "bot.html"), "text/html; charset=utf-8"));
app.MapFallbackToFile("index.html");

// Create or migrate the schema (D-034), load the 14 made-up profiles and today's generated batch before taking traffic.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await Schema.PrepareAsync(db, app.Logger);
    var demo = scope.ServiceProvider.GetRequiredService<DemoData>();
    await demo.EnsureSeedsAsync();
    await demo.RefreshIfDueAsync();
    await DemoHomes.EnsureAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>(), app.Configuration);

    // Crawl one agency by hand and exit (D-035): dotnet run -- crawl tecnocasa [--Crawl:Max=200] [--Crawl:DelaySeconds=3]
    if (args.Length >= 2 && args[0] == "crawl")
    {
        var source = HomeSources.Get(args[1]) ?? throw new ArgumentException($"No adapter for '{args[1]}'.");
        using var http = Crawl.NewClient();
        var crawl = new Crawl(db, TimeProvider.System, app.Logger, http, TimeSpan.FromSeconds(cfg.GetValue("Crawl:DelaySeconds", 3.0)));
        var r = await crawl.RunAsync(source, cfg.GetValue("Crawl:Max", 200), HomeSources.Stopped(cfg), CancellationToken.None);
        app.Logger.LogInformation("Crawl of {Source} done: {Result}", source.SourceId, r);
        Console.WriteLine($"{source.SourceId}: {r}");
        return;
    }
}

app.Run();

public partial class Program;
