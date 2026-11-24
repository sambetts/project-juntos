using Compicasa.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Compicasa.Tests;

/// <summary>Sign-up through External ID (D-033) and the schema's migrations (D-034).</summary>
public class AccountsTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public AccountsTests(AppFactory app) => _app = app;

    static readonly DateTimeOffset Now = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
    static Accounts.Ticket ATicket => new(Now, Legal.TermsVersion, Now);

    [Fact]
    public async Task External_sign_up_needs_a_ticket_and_later_sign_ins_find_the_same_account()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var oid = Guid.NewGuid().ToString();

        // Straight from External ID with no ticket: no account is made, nothing is stored.
        var (none, err) = await Accounts.FromExternalAsync(db, oid, "luz@example.com", "Luz", null, Now);
        Assert.Null(none);
        Assert.Equal("noAccount", err);
        Assert.False(await db.Accounts.AnyAsync(a => a.ExternalId == oid));

        // With a ticket: an account with the consent facts from the ticket, not yet onboarded.
        var (made, _) = await Accounts.FromExternalAsync(db, oid, " Luz@Example.com ", "Luz", ATicket, Now);
        Assert.NotNull(made);
        Assert.Equal("luz@example.com", made!.Email);
        Assert.Equal(Legal.TermsVersion, made.TermsVersion);
        Assert.Equal(Now, made.AdultConfirmedAt);
        Assert.Null(made.OnboardedAt);
        Assert.Null(made.PasswordHash);
        Assert.Equal("Luz", made.ReadProfile().Name);

        // Signing in again (with or without a ticket) finds it; a changed email follows.
        var (again, _) = await Accounts.FromExternalAsync(db, oid, "luz.nueva@example.com", "Luz", null, Now);
        Assert.Equal(made.Id, again!.Id);
        Assert.Equal("luz.nueva@example.com", again.Email);

        // Someone else can't sign up with an email that's already in use.
        var (dup, dupErr) = await Accounts.FromExternalAsync(db, Guid.NewGuid().ToString(), "luz.nueva@example.com", "Otra", ATicket, Now);
        Assert.Null(dup);
        Assert.Equal("emailTaken", dupErr);
        Assert.Equal("externalFailed", (await Accounts.FromExternalAsync(db, null, "x@example.com", "X", ATicket, Now)).Error);
    }

    [Fact]
    public void Tickets_are_signed_and_tampering_is_refused()
    {
        var dp = _app.Services.GetRequiredService<IDataProtectionProvider>();
        var raw = Accounts.Protect(dp, ATicket);
        Assert.Equal(ATicket, Accounts.Unprotect(dp, raw));
        Assert.Null(Accounts.Unprotect(dp, raw[..^4] + "AAAA"));
        Assert.Null(Accounts.Unprotect(dp, "made-up"));
        Assert.Null(Accounts.Unprotect(dp, null));
    }

    [Theory]
    [InlineData("ana@example.com", "ana@example.com")]
    [InlineData("  Ana@Example.COM ", "ana@example.com")]
    [InlineData("ana@localhost", null)]
    [InlineData("ana example@x.es", null)]
    [InlineData("not-an-email", null)]
    [InlineData("Ana <ana@example.com>", null)]
    public void Emails_are_normalised_or_refused(string raw, string? expected) =>
        Assert.Equal(expected, Accounts.NormaliseEmail(raw));

    [Fact]
    public void Every_model_change_has_a_migration()
    {
        using var db = new DesignTimeDb().CreateDbContext([]);
        Assert.False(db.Database.HasPendingModelChanges(), "The model has changes no migration covers: run `dotnet ef migrations add <Name>` in src/Compicasa.Api");
        Assert.Equal(["Initial", "EmailAccounts", "Homes", "Social", "Shortlist", "Contact", "SavedHomes", "HomePhotoUrl"], db.Database.GetMigrations().Select(m => m[15..]));
    }
}
