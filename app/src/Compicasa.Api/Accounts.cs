using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

public sealed record SignupRequest(string? Email, string? Password, string? Name, string? InviteCode, bool Adult, string? Terms);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record StartRequest(string? InviteCode, bool Adult, string? Terms);

/// <summary>
/// Accounts and sign-in (D-033). Two modes, set by Auth:Mode:
/// <list type="bullet">
/// <item>"External" (production): people sign up and sign in with their email at Microsoft Entra External ID, which
/// verifies the address and handles password resets. The app only receives a signed ID token (no client secret
/// exists). Before sending someone to sign up, the app checks the invite code and records that they confirmed
/// they're 18 or over and accepted the terms, in a short-lived signed ticket that comes back with them.</item>
/// <item>"Local" (developers and tests): email and password kept here, with the same checks.</item>
/// </list>
/// </summary>
public static class Accounts
{
    public const string ModeLocal = "Local";
    public const string ModeExternal = "External";
    const string TicketPurpose = "Compicasa.SignupTicket.v1";
    static readonly TimeSpan TicketLife = TimeSpan.FromMinutes(30);
    static readonly PasswordHasher<Account> Hasher = new();

    public sealed record Ticket(DateTimeOffset AdultAt, string TermsVersion, DateTimeOffset TermsAt);

    public static string Mode(IConfiguration cfg) =>
        string.Equals(cfg["Auth:Mode"], ModeExternal, StringComparison.OrdinalIgnoreCase) ? ModeExternal : ModeLocal;

    public static object Me(Account a) => new
    {
        id = a.Id,
        email = a.Email,
        name = a.ReadProfile().Name,
        onboarded = a.OnboardedAt is not null,
    };

    public static string? NormaliseEmail(string? raw)
    {
        var e = (raw ?? "").Trim().ToLowerInvariant();
        if (e.Length is < 3 or > 254 || e.Contains(' ')) return null;
        return MailAddress.TryCreate(e, out var m) && m.Address == e && e.Split('@')[1].Contains('.') ? e : null;
    }

    static IResult Error(string code, int status = 400) => Results.Json(new { error = code }, statusCode: status);

    /// <summary>The checks every sign-up passes, whichever mode: invite code, 18 or over, today's terms.</summary>
    static string? CheckSignup(IConfiguration cfg, string? inviteCode, bool adult, string? terms)
    {
        var code = cfg["Auth:InviteCode"];
        if (string.IsNullOrWhiteSpace(code)) return "signupClosed";
        if (inviteCode is null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(inviteCode.Trim()), Encoding.UTF8.GetBytes(code)))
            return "badInvite";
        if (!adult) return "needAdult";
        if (terms != Legal.TermsVersion) return "needTerms";
        return null;
    }

    public static async Task SignInAsync(HttpContext http, Account a) =>
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Principal(a), new AuthenticationProperties { IsPersistent = true });

    static ClaimsPrincipal Principal(Account a) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, a.Id)], CookieAuthenticationDefaults.AuthenticationScheme));

    static Account NewUser(string? email, string name, DateTimeOffset now, Ticket t)
    {
        var a = new Account
        {
            Id = "u-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8)),
            Kind = AccountKind.User,
            Email = email,
            CreatedAt = now,
            AdultConfirmedAt = t.AdultAt,
            TermsVersion = t.TermsVersion,
            TermsAcceptedAt = t.TermsAt,
        };
        a.WriteProfile(ProfileRules.Default(a.Id, name.Length > 40 ? name[..40] : name));
        return a;
    }

    public static string Protect(IDataProtectionProvider dp, Ticket t) =>
        dp.CreateProtector(TicketPurpose).ToTimeLimitedDataProtector().Protect(JsonSerializer.Serialize(t), TicketLife);

    public static Ticket? Unprotect(IDataProtectionProvider dp, string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try { return JsonSerializer.Deserialize<Ticket>(dp.CreateProtector(TicketPurpose).ToTimeLimitedDataProtector().Unprotect(raw)); }
        catch (CryptographicException) { return null; }
    }

    /// <summary>
    /// Someone came back from External ID. Find their account, or create it if they arrived with a sign-up ticket.
    /// Returns the account, or an error code for the sign-in screen.
    /// </summary>
    public static async Task<(Account? Account, string? Error)> FromExternalAsync(
        AppDb db, string? externalId, string? email, string? name, Ticket? ticket, DateTimeOffset now, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(externalId)) return (null, "externalFailed");
        var a = await db.Accounts.FirstOrDefaultAsync(x => x.ExternalId == externalId && x.Kind == AccountKind.User, ct);
        if (a is not null)
        {
            var e = NormaliseEmail(email);
            if (e is not null && e != a.Email && !await db.Accounts.AnyAsync(x => x.Email == e && x.Id != a.Id, ct))
            {
                a.Email = e; // they changed it at External ID
                await db.SaveChangesAsync(ct);
            }
            return (a, null);
        }
        if (ticket is null) return (null, "noAccount");
        var mail = NormaliseEmail(email);
        if (mail is not null && await db.Accounts.AnyAsync(x => x.Email == mail, ct)) return (null, "emailTaken");
        a = NewUser(mail, (name ?? "").Trim(), now, ticket);
        a.ExternalId = externalId;
        db.Accounts.Add(a);
        await db.SaveChangesAsync(ct);
        return (a, null);
    }

    public static void AddAuthentication(WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration;
        var auth = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
        {
            o.Cookie.Name = "compicasa";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            o.SlidingExpiration = true;
            // An API answers 401/403; it doesn't redirect to a log-in page.
            o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
            o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        if (Mode(cfg) != ModeExternal) return;

        auth.AddOpenIdConnect(o =>
        {
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.Authority = cfg["Auth:External:Authority"] ?? throw new InvalidOperationException("Auth:External:Authority is missing");
            o.ClientId = cfg["Auth:External:ClientId"] ?? throw new InvalidOperationException("Auth:External:ClientId is missing");
            // Sign-in only: an ID token posted back to us. The app calls no APIs, so it needs no client secret.
            o.ResponseType = "id_token";
            o.ResponseMode = "form_post";
            o.Scope.Clear();
            o.Scope.Add("openid");
            o.Scope.Add("profile");
            o.Scope.Add("email");
            o.MapInboundClaims = false;
            o.SaveTokens = false;
            o.GetClaimsFromUserInfoEndpoint = false;
            o.CallbackPath = "/signin-oidc";
            o.SignedOutCallbackPath = "/signout-callback-oidc";
            o.SignedOutRedirectUri = "/";
            o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            o.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Events.OnRedirectToIdentityProvider = ctx =>
            {
                if (ctx.Properties.Items.TryGetValue("lang", out var lang)) ctx.ProtocolMessage.UiLocales = lang == "en" ? "en" : "es";
                // prompt=create should open the sign-up page, but External ID has ignored it since early 2026 (Reported, Microsoft Q&A),
                // so the sign-up screen also tells people to tap "No account? Create one". Kept in case it starts working again.
                if (ctx.Properties.Items.ContainsKey("ticket")) ctx.ProtocolMessage.Prompt = "create";
                return Task.CompletedTask;
            };
            o.Events.OnTokenValidated = async ctx =>
            {
                var http = ctx.HttpContext;
                var db = http.RequestServices.GetRequiredService<AppDb>();
                var dp = http.RequestServices.GetRequiredService<IDataProtectionProvider>();
                var clock = http.RequestServices.GetRequiredService<TimeProvider>();
                var p = ctx.Principal!;
                var oid = p.FindFirstValue("oid") ?? p.FindFirstValue("sub");
                var email = p.FindFirstValue("email") ?? p.FindFirstValue("preferred_username");
                ctx.Properties!.Items.TryGetValue("ticket", out var raw);
                var (a, error) = await FromExternalAsync(db, oid, email, p.FindFirstValue("name"), Unprotect(dp, raw), clock.GetUtcNow(), http.RequestAborted);
                if (a is null)
                {
                    ctx.HandleResponse();
                    http.Response.Redirect("/#authError=" + error);
                    return;
                }
                // Keep only our own account ID in the cookie; nothing else from the token is stored.
                ctx.Principal = Principal(a);
                ctx.Properties.IsPersistent = true;
                ctx.Properties.RedirectUri = "/";
            };
            o.Events.OnRemoteFailure = ctx =>
            {
                // Includes someone cancelling at the sign-in page.
                ctx.HandleResponse();
                ctx.Response.Redirect("/#authError=externalFailed");
                return Task.CompletedTask;
            };
        });
    }

    public static void Map(WebApplication app)
    {
        var cfg = app.Configuration;
        var external = Mode(cfg) == ModeExternal;

        app.MapGet("/api/config", () => Results.Ok(new
        {
            auth = external ? "external" : "local",
            invite = !string.IsNullOrWhiteSpace(cfg["Auth:InviteCode"]),
            terms = Legal.TermsVersion,
        }));

        var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth");

        if (!external)
        {
            auth.MapPost("/signup", async (SignupRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
            {
                if (CheckSignup(cfg, r.InviteCode, r.Adult, r.Terms) is { } problem) return Error(problem, problem is "signupClosed" or "badInvite" ? 403 : 400);
                var email = NormaliseEmail(r.Email);
                if (email is null) return Error("badEmail");
                if ((r.Password ?? "").Length < 10 || r.Password!.Length > 200) return Error("badPassword");
                var name = (r.Name ?? "").Trim();
                if (name.Length is < 1 or > 40) return Error("badName");
                if (await db.Accounts.AnyAsync(a => a.Email == email)) return Error("emailTaken", 409);
                var now = clock.GetUtcNow();
                var a = NewUser(email, name, now, new Ticket(now, Legal.TermsVersion, now));
                a.PasswordHash = Hasher.HashPassword(a, r.Password);
                db.Accounts.Add(a);
                await db.SaveChangesAsync();
                await SignInAsync(http, a);
                return Results.Ok(Me(a));
            });

            auth.MapPost("/login", async (LoginRequest r, HttpContext http, AppDb db) =>
            {
                var email = NormaliseEmail(r.Email) ?? "";
                var a = await db.Accounts.FirstOrDefaultAsync(x => x.Email == email && x.Kind == AccountKind.User);
                if (a?.PasswordHash is null || Hasher.VerifyHashedPassword(a, a.PasswordHash, r.Password ?? "") == PasswordVerificationResult.Failed)
                    return Error("badLogin", 401);
                await SignInAsync(http, a);
                return Results.Ok(Me(a));
            });
        }
        else
        {
            // Step 1 of an External ID sign-up: check what only we can check, then hand back a ticket for the trip.
            auth.MapPost("/start", (StartRequest r, IDataProtectionProvider dp, TimeProvider clock) =>
            {
                if (CheckSignup(cfg, r.InviteCode, r.Adult, r.Terms) is { } problem) return Error(problem, problem is "signupClosed" or "badInvite" ? 403 : 400);
                var now = clock.GetUtcNow();
                return Results.Ok(new { ticket = Protect(dp, new Ticket(now, Legal.TermsVersion, now)) });
            });

            // Step 2: off to External ID (sign-up if there's a ticket, sign-in otherwise).
            app.MapGet("/auth/external", (string? ticket, string? lang, IDataProtectionProvider dp) =>
            {
                var props = new AuthenticationProperties { RedirectUri = "/" };
                props.Items["lang"] = lang == "en" ? "en" : "es";
                if (!string.IsNullOrEmpty(ticket))
                {
                    if (Unprotect(dp, ticket) is null) return Results.Redirect("/#authError=badTicket");
                    props.Items["ticket"] = ticket;
                }
                return Results.Challenge(props, [OpenIdConnectDefaults.AuthenticationScheme]);
            }).RequireRateLimiting("auth");

            app.MapGet("/auth/signout", () => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));
        }

        auth.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // With External ID the browser also ends its session there, so the next sign-in asks again.
            return Results.Ok(new { next = external ? "/auth/signout" : null });
        });
    }
}

/// <summary>The terms and privacy notice people accept. Drafts until the lawyer's review and B-020.</summary>
public static class Legal
{
    public const string TermsVersion = "2026-11-22-draft";
}
