using Compicasa.Api;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Compicasa.Tests;

/// <summary>Session 32 code review: the in-memory copies of the made-up people and of the home index.</summary>
public class CacheTests
{
    /// <summary>A private in-memory database (the caches are kept per connection string).</summary>
    static (SqliteConnection Conn, AppDb Db) NewDb()
    {
        var conn = new SqliteConnection($"Data Source=cache{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        conn.Open();
        var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        return (conn, db);
    }

    static Home NewHome(string id, string source, DateTimeOffset now) =>
        new() { Id = id, Source = source, Agency = "Agencia", Url = "https://www.tecnocasa.es/venta/piso/x/y/1.html", Beds = 3, FirstSeenAt = now, LastSeenAt = now, CheckedAt = now };

    static IConfiguration Cfg(string? stopped = null) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Homes:CacheSeconds"] = "300", ["Homes:Stopped"] = stopped }).Build();

    [Fact]
    public async Task The_home_index_is_read_again_only_after_its_copy_expires()
    {
        var (conn, db) = NewDb();
        using var _ = conn; using var __ = db;
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        db.Homes.Add(NewHome("tecnocasa:1", "tecnocasa", clock.GetUtcNow()));
        await db.SaveChangesAsync();
        Assert.Single(await HomeIndex.LiveAsync(db, Cfg(), clock, default));

        db.Homes.Add(NewHome("tecnocasa:2", "tecnocasa", clock.GetUtcNow()));
        await db.SaveChangesAsync();
        Assert.Single(await HomeIndex.LiveAsync(db, Cfg(), clock, default)); // the copy is still fresh
        // A stopped agency is hidden at once, copy or not.
        Assert.Empty(await HomeIndex.LiveAsync(db, Cfg(stopped: "tecnocasa"), clock, default));

        clock.Advance(TimeSpan.FromSeconds(301));
        Assert.Equal(2, (await HomeIndex.LiveAsync(db, Cfg(), clock, default)).Count);
    }

    [Fact]
    public async Task Made_up_people_are_read_again_when_a_new_batch_is_loaded()
    {
        var (conn, db) = NewDb();
        using var _ = conn; using var __ = db;
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        var demo = new DemoData(db, clock, NullLogger<DemoData>.Instance);
        await demo.EnsureSeedsAsync();
        await demo.RefreshIfDueAsync();
        var first = (await DemoProfiles.GetAsync(db, default)).Select(p => p.Account.Id).ToHashSet();
        Assert.Equal(14 + Compicasa.Core.DemoGenerator.BatchSize, first.Count);
        Assert.Same(await DemoProfiles.GetAsync(db, default), await DemoProfiles.GetAsync(db, default));

        clock.Advance(TimeSpan.FromDays(1));
        Assert.True(await demo.RefreshIfDueAsync());
        var second = (await DemoProfiles.GetAsync(db, default)).Select(p => p.Account.Id).ToHashSet();
        Assert.Equal(first.Count, second.Count);
        Assert.NotEqual(first, second); // yesterday's generated people are gone, today's are in
        Assert.Equal(14, first.Intersect(second).Count()); // the seed profiles stay
    }
}
