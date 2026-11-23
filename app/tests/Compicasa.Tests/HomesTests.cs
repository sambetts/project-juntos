using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Compicasa.Api;
using Compicasa.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Compicasa.Tests;

/// <summary>The homes index (D-035): fit rules, Madrid areas, robots.txt, the Tecnocasa adapter and the crawl. Made-up data only.</summary>
public class HomesTests
{
    static HouseBrief Brief(int beds = 3, int baths = 2, int cars = 0, string[]? areas = null, string[]? types = null, FeatureNeed[]? feats = null) =>
        new(beds, baths, 0, baths, cars, 0, 0, beds, areas ?? ["south"], types ?? ["flat"], feats ?? [], [], 90, 80, 100, false);

    static HomeListing Listing(int? beds = 3, int? baths = 2, int? m2 = 90, string type = "flat", string? area = "south", params string[] feats) =>
        new("t:1", "tecnocasa", "Tecnocasa · Oficina", "https://www.tecnocasa.es/venta/piso/madrid/getafe/1.html", 200000, m2, beds, baths, type,
            "Getafe", "28901", null, area, feats, "E", false);

    [Fact]
    public void Hard_rules_are_bedrooms_type_and_area()
    {
        Assert.True(HomeFit.Fit(Brief(), 80, 100, Listing()).Ok);
        Assert.False(HomeFit.Fit(Brief(), 80, 100, Listing(beds: 2)).Ok);
        Assert.False(HomeFit.Fit(Brief(), 80, 100, Listing(beds: null)).Ok);
        Assert.False(HomeFit.Fit(Brief(), 80, 100, Listing(type: "house")).Ok);
        Assert.False(HomeFit.Fit(Brief(), 80, 100, Listing(area: "north")).Ok);
        Assert.False(HomeFit.Fit(Brief(), 80, 100, Listing(area: null)).Ok);
        Assert.True(HomeFit.Fit(Brief(areas: [], types: []), 80, 100, Listing(type: "house", area: null)).Ok);
    }

    [Fact]
    public void Score_falls_for_missing_bathrooms_size_and_must_haves()
    {
        var full = HomeFit.Fit(Brief(feats: [new("lift", "must", ["a"], [])]), 80, 100, Listing(feats: "lift"));
        Assert.Equal(100, full.Score);
        Assert.Contains("f:lift", full.Fits);
        var noLift = HomeFit.Fit(Brief(feats: [new("lift", "must", ["a"], [])]), 80, 100, Listing());
        Assert.Contains("f:lift", noLift.Misses);
        Assert.True(noLift.Score < full.Score);
        var small = HomeFit.Fit(Brief(), 80, 100, Listing(m2: 60));
        Assert.Contains("small", small.Misses);
        var oneBath = HomeFit.Fit(Brief(), 80, 100, Listing(baths: 1));
        Assert.Contains("baths", oneBath.Misses);
        // Parking is needed when anyone has a car, even if nobody ticked it.
        Assert.Contains("f:parking", HomeFit.Fit(Brief(cars: 1), 80, 100, Listing()).Misses);
        // Things an advert can't tell us are left for the group to check.
        Assert.Contains("f:transport", HomeFit.Fit(Brief(feats: [new("transport", "must", ["a"], [])]), 80, 100, Listing()).Check);
    }

    [Fact]
    public void Madrid_areas_come_from_town_and_postcode()
    {
        Assert.Equal("centre", MadridZones.For("Madrid", "28005"));
        Assert.Equal("city", MadridZones.For("Madrid", "28047"));
        Assert.Equal("east", MadridZones.For("Alcalá De Henares", "28806"));
        Assert.Equal("south", MadridZones.For("Leganés", "28913"));
        Assert.Equal("north", MadridZones.For("Las Rozas de Madrid", "28232"));
        Assert.Null(MadridZones.For("Villanueva De Perales", "28609"));
    }

    [Fact]
    public void Robots_txt_is_honoured()
    {
        var r = Robots.Parse("User-agent: *\nDisallow: /venta\nAllow: /venta/piso\nDisallow: /*?q=\n", "CompicasaBot");
        Assert.False(r.Allows(new Uri("https://x.es/venta/local/1.html")));
        Assert.True(r.Allows(new Uri("https://x.es/venta/piso/1.html")));
        Assert.False(r.Allows(new Uri("https://x.es/buscar?q=1")));
        Assert.True(r.Allows(new Uri("https://x.es/")));
        var mine = Robots.Parse("User-agent: *\nDisallow:\n\nUser-agent: CompicasaBot\nDisallow: /\n", "CompicasaBot");
        Assert.False(mine.Allows(new Uri("https://x.es/venta/piso/1.html")));
        Assert.True(Robots.Parse(null, "CompicasaBot").Allows(new Uri("https://x.es/a")));
    }

    [Fact]
    public void Portals_are_off_limits_and_stopped_agencies_are_read_from_settings()
    {
        Assert.True(HomeSources.IsPortal(new Uri("https://www.idealista.com/inmueble/1/")));
        Assert.True(HomeSources.IsPortal(new Uri("https://fotocasa.es/")));
        Assert.False(HomeSources.IsPortal(new Uri("https://www.tecnocasa.es/")));
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Homes:Stopped"] = "tecnocasa, other" }).Build();
        Assert.Contains("Tecnocasa", HomeSources.Stopped(cfg));
    }

    /// <summary>A made-up advert page shaped like Tecnocasa's (values invented; no real advert copied).</summary>
    internal static string AdvertPage(int id, string contract = "venta", string town = "Getafe", string postcode = "28901", int beds = 3) =>
        $"<html><body><estate-show :estate=\"{WebUtility.HtmlEncode(JsonSerializer.Serialize(new
        {
            id,
            detail_url = $"https://www.tecnocasa.es/venta/piso/madrid/getafe/{id}.html",
            contract = new { slug = contract },
            sector = new { id = "res" },
            type = new { slug = "piso" },
            city = new { title = town },
            province = new { title = "Madrid" },
            district = new { title = "Centro" },
            numeric_price = 210000,
            numeric_surface = "88.00",
            bathrooms = "2 baños",
            rooms = $"{beds} dorm.",
            description = "<p>Texto de la agencia que no guardamos</p>",
            media = new { images = new[] { new { url = "https://example.invalid/photo.jpg" } } },
            energy_data = new { @class = "d" },
            features = new { elevator = "Sí", garden = "", terraces = 1, car_places = (int?)null, box = (string?)null },
            agency = new { name = "Oficina Ejemplo SL", phone = "+34 600 000 000" },
            data = new object[] { new { label = "Cap", valore = postcode }, new { label = "Dormitorios", valore = beds } },
        }))}\"></estate-show></body></html>";

    [Fact]
    public void Tecnocasa_adverts_give_facts_agency_and_link_only()
    {
        var h = new TecnocasaSource().Parse("https://www.tecnocasa.es/venta/piso/madrid/getafe/7.html", AdvertPage(7))!;
        Assert.Equal("tecnocasa:7", h.Id);
        Assert.Equal("Tecnocasa · Oficina Ejemplo SL", h.Agency);
        Assert.Equal("https://www.tecnocasa.es/venta/piso/madrid/getafe/7.html", h.Url);
        Assert.Equal((210000, 88, 3, 2, "flat"), (h.Price!.Value, h.M2!.Value, h.Beds!.Value, h.Baths!.Value, h.Type!));
        Assert.Equal(("Getafe", "28901", "south", "D"), (h.Municipality!, h.Postcode!, h.Area!, h.Energy!));
        Assert.Equal("lift,terrace", h.Features);
        // Nothing beyond the facts: no description, photo or phone ends up anywhere on the record.
        var stored = JsonSerializer.Serialize(h);
        Assert.DoesNotContain("Texto de la agencia", stored);
        Assert.DoesNotContain("photo.jpg", stored);
        Assert.DoesNotContain("600 000 000", stored);
        Assert.Null(new TecnocasaSource().Parse("https://www.tecnocasa.es/x.html", AdvertPage(8, contract: "alquiler")));
    }

    static byte[] Sitemap(params (string Url, string LastMod)[] urls)
    {
        var xml = "<?xml version=\"1.0\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"
            + string.Concat(urls.Select(u => $"<url><loc>{u.Url}</loc><lastmod>{u.LastMod}</lastmod></url>")) + "</urlset>";
        var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true)) gz.Write(Encoding.UTF8.GetBytes(xml));
        return ms.ToArray();
    }

    sealed class FakeSite(Dictionary<string, byte[]> pages) : HttpMessageHandler
    {
        public List<string> Requested { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            Requested.Add(url);
            return Task.FromResult(pages.TryGetValue(url, out var b)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(b) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    static string Advert(int id) => $"https://www.tecnocasa.es/venta/piso/madrid/getafe/{id}.html";

    [Fact]
    public async Task Made_up_homes_are_added_by_default_and_deleted_when_switched_off()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));
        IConfiguration Cfg(string? demo) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Homes:Demo"] = demo }).Build();

        await DemoHomes.EnsureAsync(db, clock, Cfg(null));
        Assert.Equal(90, await db.Homes.CountAsync(h => h.Source == HomeSources.Demo));
        db.Homes.Add(new Home { Id = "tecnocasa:1", Source = "tecnocasa", Agency = "Tecnocasa", Url = "https://www.tecnocasa.es/venta/piso/madrid/getafe/1.html", FirstSeenAt = clock.GetUtcNow(), LastSeenAt = clock.GetUtcNow(), CheckedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync();

        // The live site (F-019): made-up homes go, real ones stay.
        await DemoHomes.EnsureAsync(db, clock, Cfg("false"));
        Assert.Equal(0, await db.Homes.CountAsync(h => h.Source == HomeSources.Demo));
        Assert.Equal(1, await db.Homes.CountAsync());
        Assert.False(DemoHomes.Enabled(Cfg("False")));
        Assert.True(DemoHomes.Enabled(Cfg("true")));
    }

    [Fact]
    public async Task A_crawl_stores_new_adverts_skips_unchanged_ones_honours_robots_and_marks_gone_ones()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));
        var pages = new Dictionary<string, byte[]>
        {
            ["https://www.tecnocasa.es/robots.txt"] = Encoding.UTF8.GetBytes("User-agent: *\nDisallow: /venta/piso/madrid/getafe/3.html\n"),
            ["https://www.tecnocasa.es/sitemap/inmuebles-0.xml.gz"] = Sitemap(
                (Advert(1), "2026-11-21T00:00:00+01:00"), (Advert(2), "2026-11-21T00:00:00+01:00"), (Advert(3), "2026-11-21T00:00:00+01:00"),
                ("https://www.tecnocasa.es/venta/local-comercial/madrid/getafe/9.html", "2026-11-21T00:00:00+01:00"),
                ("https://www.tecnocasa.es/alquiler/piso/madrid/getafe/10.html", "2026-11-21T00:00:00+01:00")),
            [Advert(1)] = Encoding.UTF8.GetBytes(AdvertPage(1)),
            [Advert(2)] = Encoding.UTF8.GetBytes(AdvertPage(2)),
            [Advert(3)] = Encoding.UTF8.GetBytes(AdvertPage(3)),
        };
        var site = new FakeSite(pages);
        var crawl = new Crawl(db, clock, NullLogger.Instance, new HttpClient(site), TimeSpan.Zero);

        var first = await crawl.RunAsync(new TecnocasaSource(), 100, new HashSet<string>(), default);
        Assert.Equal((3, 2, 1), (first.Listed, first.Stored, first.Blocked));
        Assert.DoesNotContain(Advert(3), site.Requested);
        Assert.DoesNotContain(site.Requested, u => u.Contains("local-comercial") || u.Contains("alquiler"));

        // Next day: advert 2 left the sitemap; advert 1 hasn't changed, so it isn't fetched again.
        clock.Advance(TimeSpan.FromDays(1));
        pages["https://www.tecnocasa.es/sitemap/inmuebles-0.xml.gz"] = Sitemap((Advert(1), "2026-11-21T00:00:00+01:00"));
        site.Requested.Clear();
        var second = await crawl.RunAsync(new TecnocasaSource(), 100, new HashSet<string>(), default);
        Assert.Equal((1, 0, 1), (second.Unchanged, second.Fetched, second.Gone));
        Assert.DoesNotContain(Advert(1), site.Requested);
        Assert.NotNull((await db.Homes.FindAsync("tecnocasa:2"))!.GoneAt);
        Assert.Null((await db.Homes.FindAsync("tecnocasa:1"))!.GoneAt);

        // An agency that asked us to stop is never crawled.
        await Assert.ThrowsAsync<InvalidOperationException>(() => crawl.RunAsync(new TecnocasaSource(), 100, new HashSet<string> { "tecnocasa" }, default));
    }
}

public class HomesApiTests : IClassFixture<AppFactory>
{
    readonly AppFactory _app;
    public HomesApiTests(AppFactory app) => _app = app;
    static int _n;

    [Fact]
    public async Task Group_members_see_fitting_homes_with_their_agency_and_outsiders_see_nothing()
    {
        async Task<(HttpClient C, string Id)> Person(string name)
        {
            var c = _app.Browser();
            var id = await AppFactory.SignUpAsync(c, $"homes{Interlocked.Increment(ref _n)}-{Guid.NewGuid():N}@example.com", name);
            await c.PutAsJsonAsync("/api/profile", new
            {
                name, household = "single", bedrooms = 1, privateBath = false, cars = 0, pets = Array.Empty<string>(), acceptsPets = true,
                smoking = "no", acceptsSmoking = "outside", sleep = 3, tidy = 3, social = 3, guests = 3, meals = 3, wfh = false, horizon = "5-9",
                areas = new[] { "centre", "city", "north", "south", "east" }, types = new[] { "flat", "house" },
                needs = new Dictionary<string, string>(), open = new[] { "single", "couple", "family", "singleParent" }, maxHouseholds = 4,
            });
            return (c, id);
        }
        var (a, aId) = await Person("Ana");
        var (b, bId) = await Person("Bea");
        await a.PutAsJsonAsync($"/api/reactions/{bId}", new { kind = "like" });
        await b.PutAsJsonAsync($"/api/reactions/{aId}", new { kind = "like" });
        var g = await (await a.PostAsJsonAsync("/api/groups", new { ids = new[] { bId } })).Content.ReadFromJsonAsync<JsonElement>();
        var gid = g.GetProperty("id").GetString();

        var r = await a.GetFromJsonAsync<JsonElement>($"/api/groups/{gid}/homes");
        Assert.True(r.GetProperty("considered").GetInt32() > 0);
        var homes = r.GetProperty("homes").EnumerateArray().ToList();
        Assert.NotEmpty(homes);
        Assert.All(homes, h =>
        {
            Assert.True(h.GetProperty("beds").GetInt32() >= 2);
            Assert.False(string.IsNullOrEmpty(h.GetProperty("agency").GetString()));
            Assert.True(h.GetProperty("isDemo").GetBoolean() || h.GetProperty("url").GetString()!.StartsWith("https://"));
        });
        // Real homes first, then made-up ones; best first within each.
        var order = homes.Select(h => (Demo: h.GetProperty("isDemo").GetBoolean(), Score: h.GetProperty("score").GetInt32())).ToList();
        Assert.Equal(order.OrderBy(x => x.Demo).ThenByDescending(x => x.Score), order);

        var outsider = _app.Browser();
        await AppFactory.SignUpAsync(outsider, $"homes-out-{Guid.NewGuid():N}@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/groups/{gid}/homes")).StatusCode);

        // The Home page (Session 20): a few homes for you and your best match, before any group is needed.
        var mine = await a.GetFromJsonAsync<JsonElement>("/api/me/homes");
        Assert.Equal(bId, mine.GetProperty("with").GetProperty("id").GetString());
        Assert.True(mine.GetProperty("with").GetProperty("mutual").GetBoolean());
        var few = mine.GetProperty("homes").EnumerateArray().ToList();
        Assert.InRange(few.Count, 1, 6);
        Assert.All(few, h => Assert.True(h.GetProperty("beds").GetInt32() >= 2));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Browser().GetAsync("/api/me/homes")).StatusCode);
    }

    [Fact]
    public async Task The_bot_page_says_how_to_block_the_crawler()
    {
        var html = await _app.CreateClient().GetStringAsync("/bot");
        Assert.Contains("User-agent: CompicasaBot", html);
        Assert.Contains(Crawl.UserAgent.Split(' ')[0], "CompicasaBot/0.1");
    }
}
