using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// Where homes come from (D-035): agencies' own websites, never portals. Every home keeps its agency's name and
/// the link to its advert. Any agency named in the "Homes:Stopped" setting (comma-separated source ids) is hidden
/// from groups at once and never crawled again, for example after it asks us to stop.
/// </summary>
public static partial class HomeSources
{
    public const string Demo = "demo";

    /// <summary>Portals and aggregators: off-limits (AGENTS.md section 4, D-035). Checked against every host we fetch.</summary>
    public static readonly string[] Portals =
    [
        "idealista.com", "idealista.es", "fotocasa.es", "habitaclia.com", "pisos.com", "milanuncios.com", "yaencontre.com",
        "kyero.com", "thinkspain.com", "spainhouses.net", "enalquiler.com", "trovit.es", "mitula.com", "nestoria.es",
        "casafari.com", "tucasa.com", "globaliza.com", "housfy.com",
    ];

    public static bool IsPortal(Uri u) => Portals.Any(p => u.Host.Equals(p, StringComparison.OrdinalIgnoreCase) || u.Host.EndsWith("." + p, StringComparison.OrdinalIgnoreCase));

    public static HashSet<string> Stopped(IConfiguration cfg) =>
        new((cfg["Homes:Stopped"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);

    /// <summary>The adapters we have. One per agency network.</summary>
    public static IHomeSource? Get(string id) => id.ToLowerInvariant() switch
    {
        TecnocasaSource.Id => new TecnocasaSource(),
        _ => null,
    };

    /// <summary>Every agency network the daily crawl reads, in the order the "Where homes come from" page lists them.
    /// Add a new adapter here, in Get and in the WebJob's run.cmd.</summary>
    public static readonly string[] Crawled = [TecnocasaSource.Id];

    public sealed record Summary(string Id, string Name, string Site, int Homes, DateTimeOffset? LastChecked);

    /// <summary>For the public "Where homes come from" page (#homes-sources): each agency we read, how many of its homes
    /// are listed now and when the crawl last saw them. Stopped agencies are left out.</summary>
    public static async Task<List<Summary>> SummariesAsync(AppDb db, IConfiguration cfg, CancellationToken ct)
    {
        var stopped = Stopped(cfg);
        var rows = new List<Summary>();
        foreach (var id in Crawled.Where(id => !stopped.Contains(id)))
        {
            var src = Get(id)!;
            var live = db.Homes.AsNoTracking().Where(h => h.Source == id && h.GoneAt == null);
            var count = await live.CountAsync(ct);
            // SQLite (local and tests) can't take the maximum of a DateTimeOffset in the database; SQL Server can.
            DateTimeOffset? last = db.Database.IsSqlServer()
                ? await live.MaxAsync(h => (DateTimeOffset?)h.LastSeenAt, ct)
                : (await live.Select(h => h.LastSeenAt).ToListAsync(ct)).Cast<DateTimeOffset?>().Max();
            rows.Add(new Summary(id, src.Name, src.Site.ToString(), count, last));
        }
        return rows;
    }
}

public sealed record Discovered(string Url, DateTimeOffset? LastModified);

/// <summary>One agency network: how to find its adverts and read the facts from one.</summary>
public interface IHomeSource
{
    string SourceId { get; }
    /// <summary>The agency network's name as people know it, shown on the "Where homes come from" page.</summary>
    string Name { get; }
    Uri Site { get; }
    IAsyncEnumerable<Discovered> DiscoverAsync(Func<Uri, CancellationToken, Task<byte[]?>> fetch, CancellationToken ct);
    /// <summary>The facts from one advert page, or null if it isn't a home for sale we can use.</summary>
    Home? Parse(string url, string html);
}

/// <summary>
/// Tecnocasa (tecnocasa.es). Its sitemaps list every advert with the date it last changed, and each advert page
/// carries its facts as JSON in an ":estate" attribute. We keep homes for sale anywhere in Spain (B-059; Madrid only until then).
/// </summary>
public sealed partial class TecnocasaSource : IHomeSource
{
    public const string Id = "tecnocasa";
    public string SourceId => Id;
    public string Name => "Tecnocasa";
    public Uri Site { get; } = new("https://www.tecnocasa.es/");
    static readonly string[] Sitemaps = ["sitemap/inmuebles-0.xml.gz", "sitemap/inmuebles-1.xml.gz"];

    static readonly Dictionary<string, string> TypeOf = new()
    {
        ["piso"] = "flat", ["atico"] = "flat", ["duplex"] = "flat", ["estudio"] = "flat", ["loft"] = "flat", ["apartamento"] = "flat",
        ["chalet"] = "house", ["casa"] = "house", ["adosado"] = "house", ["pareado"] = "house", ["casa-rural"] = "house", ["unifamiliar"] = "house",
        ["chalet-adosado"] = "house", ["chalet-pareado"] = "house", ["casa-unifamiliar"] = "house",
    };

    [GeneratedRegex(@"^https://www\.tecnocasa\.es/venta/(?<type>[a-z-]+)/[a-z0-9-]+/[a-z0-9-]+/\d+\.html$")]
    private static partial Regex AdvertUrl();

    public async IAsyncEnumerable<Discovered> DiscoverAsync(Func<Uri, CancellationToken, Task<byte[]?>> fetch, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var path in Sitemaps)
        {
            var bytes = await fetch(new Uri(Site, path), ct);
            if (bytes is null) continue;
            foreach (var d in Crawl.ReadSitemap(bytes))
            {
                var m = AdvertUrl().Match(d.Url);
                if (m.Success && TypeOf.ContainsKey(m.Groups["type"].Value)) yield return d;
            }
        }
    }

    public Home? Parse(string url, string html)
    {
        var m = Regex.Match(html, @":estate=""([^""]+)""");
        if (!m.Success) return null;
        using var doc = JsonDocument.Parse(WebUtility.HtmlDecode(m.Groups[1].Value));
        var e = doc.RootElement;
        if (Str(e, "contract", "slug") != "venta") return null;
        if (Str(e, "sector", "id") is { } sector && sector != "res") return null;
        if (!TypeOf.TryGetValue(Str(e, "type", "slug") ?? "", out var type)) return null;
        var data = e.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array
            ? d.EnumerateArray().Where(x => x.TryGetProperty("label", out _)).GroupBy(x => x.GetProperty("label").GetString() ?? "").ToDictionary(g => g.Key, g => g.First().GetProperty("valore"))
            : [];
        var town = Str(e, "city", "title");
        var postcode = data.TryGetValue("Cap", out var cap) ? Text(cap) : null;
        var f = e.TryGetProperty("features", out var ff) ? ff : default;
        var features = new List<string>();
        if (Yes(f, "elevator")) features.Add("lift");
        if (Yes(f, "garden")) features.Add("garden");
        if (Yes(f, "terraces")) features.Add("terrace");
        if (Yes(f, "car_places") || Yes(f, "box")) features.Add("parking");
        var agencyName = e.TryGetProperty("agency", out var ag) ? Str(ag, "name") : null;
        var reference = e.TryGetProperty("id", out var idEl) ? Text(idEl) : null;
        if (reference is null) return null;
        return new Home
        {
            Id = $"{Id}:{reference}",
            Source = Id,
            Agency = string.IsNullOrWhiteSpace(agencyName) ? "Tecnocasa" : Crawl.Clip($"Tecnocasa · {agencyName.Trim()}", 120)!,
            Url = Str(e, "detail_url") is { } u && u.StartsWith("https://www.tecnocasa.es/", StringComparison.Ordinal) ? u : url,
            Price = Crawl.Int(Text(e.TryGetProperty("numeric_price", out var p) ? p : default)),
            M2 = Crawl.Int(Text(e.TryGetProperty("numeric_surface", out var s) ? s : default)),
            Beds = Crawl.Int(data.TryGetValue("Dormitorios", out var beds) ? Text(beds) : Str(e, "rooms")),
            Baths = Crawl.Int(Str(e, "bathrooms")),
            Type = type,
            Province = Str(e, "province", "title"),
            Municipality = town,
            Postcode = postcode,
            District = Str(e, "district", "title") ?? Str(e, "quarter"),
            Area = Regions.AreaFor(town, postcode),
            Features = string.Join(',', features),
            Energy = Str(e, "energy_data", "class")?.ToUpperInvariant() is { Length: 1 } en && "ABCDEFG".Contains(en) ? en : null,
        };
    }

    static string? Str(JsonElement e, params string[] path)
    {
        foreach (var k in path)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(k, out e)) return null;
        }
        return Text(e);
    }

    static string? Text(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => string.IsNullOrWhiteSpace(e.GetString()) ? null : e.GetString(),
        JsonValueKind.Number => e.GetRawText(),
        _ => null,
    };

    /// <summary>A feature is present if its value is a positive number or a non-empty text other than "no".</summary>
    static bool Yes(JsonElement f, string key)
    {
        if (f.ValueKind != JsonValueKind.Object || !f.TryGetProperty(key, out var v)) return false;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDouble() > 0,
            JsonValueKind.String => v.GetString() is { } t && t.Trim().Length > 0 && !t.Trim().Equals("no", StringComparison.OrdinalIgnoreCase) && t.Trim() != "0",
            JsonValueKind.True => true,
            _ => false,
        };
    }
}

/// <summary>Honours robots.txt: the group for our bot if there is one, otherwise "*"; the longest matching rule wins.</summary>
public sealed class Robots
{
    readonly List<(bool Allow, string Pattern)> _rules = [];

    public static Robots Parse(string? text, string agent)
    {
        var r = new Robots();
        if (string.IsNullOrEmpty(text)) return r;
        var groups = new List<(List<string> Agents, List<(bool, string)> Rules)>();
        (List<string> Agents, List<(bool, string)> Rules)? cur = null;
        var lastWasAgent = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            var i = line.IndexOf(':');
            if (i < 0) continue;
            var key = line[..i].Trim().ToLowerInvariant();
            var val = line[(i + 1)..].Trim();
            if (key == "user-agent")
            {
                if (cur is null || !lastWasAgent) groups.Add((cur = ([], [])).Value);
                cur!.Value.Agents.Add(val.ToLowerInvariant());
                lastWasAgent = true;
            }
            else if (key is "allow" or "disallow" && cur is not null)
            {
                if (val.Length > 0) cur.Value.Rules.Add((key == "allow", val));
                lastWasAgent = false;
            }
            else lastWasAgent = false;
        }
        var mine = groups.Where(g => g.Agents.Any(a => a != "*" && agent.Contains(a, StringComparison.OrdinalIgnoreCase))).ToList();
        if (mine.Count == 0) mine = groups.Where(g => g.Agents.Contains("*")).ToList();
        foreach (var g in mine) r._rules.AddRange(g.Rules);
        return r;
    }

    public bool Allows(Uri u)
    {
        var path = u.PathAndQuery;
        var best = (Allow: true, Length: -1);
        foreach (var (allow, pattern) in _rules)
        {
            if (!Matches(pattern, path)) continue;
            if (pattern.Length > best.Length || (pattern.Length == best.Length && allow)) best = (allow, pattern.Length);
        }
        return best.Allow;
    }

    static bool Matches(string pattern, string path)
    {
        var anchored = pattern.EndsWith('$');
        var rx = "^" + Regex.Escape(anchored ? pattern[..^1] : pattern).Replace(@"\*", ".*") + (anchored ? "$" : "");
        return Regex.IsMatch(path, rx);
    }
}

/// <summary>
/// Runs one crawl of one agency (D-035), by hand for now: `dotnet run -- crawl tecnocasa`.
/// Slow and polite: one request at a time with a pause between them, robots.txt honoured, our own user agent.
/// Only adverts that changed since we last read them are fetched; adverts that left the sitemap are marked gone.
/// </summary>
public sealed class Crawl(AppDb db, TimeProvider clock, ILogger log, HttpClient http, TimeSpan delay)
{
    public const string UserAgent = "CompicasaBot/0.1 (+https://app-compicasa-prod-25yr5.azurewebsites.net/bot; facts and a link to the advert only)";

    public sealed record Result(int Listed, int Fetched, int Stored, int Unchanged, int Skipped, int Gone, int Blocked);

    public static HttpClient NewClient()
    {
        var h = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All, PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
        { Timeout = TimeSpan.FromSeconds(30) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        h.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-ES");
        return h;
    }

    public async Task<Result> RunAsync(IHomeSource source, int max, ISet<string> stopped, CancellationToken ct)
    {
        if (HomeSources.IsPortal(source.Site)) throw new InvalidOperationException($"{source.Site.Host} is a portal: off-limits (D-035).");
        if (stopped.Contains(source.SourceId)) throw new InvalidOperationException($"{source.SourceId} is in Homes:Stopped.");

        var robotsText = await GetTextAsync(new Uri(source.Site, "/robots.txt"), ct);
        var robots = Robots.Parse(robotsText, "CompicasaBot");
        var first = true;
        async Task<byte[]?> Fetch(Uri u, CancellationToken c)
        {
            if (HomeSources.IsPortal(u) || u.Host != source.Site.Host) return null;
            if (!robots.Allows(u)) return null;
            if (!first) await Task.Delay(delay, c);
            first = false;
            using var r = await http.GetAsync(u, c);
            if (r.StatusCode == HttpStatusCode.TooManyRequests || (int)r.StatusCode == 503)
                throw new InvalidOperationException($"{u.Host} asked us to slow down ({(int)r.StatusCode}); stopping this run.");
            if (!r.IsSuccessStatusCode) { log.LogInformation("{Status} for {Url}", (int)r.StatusCode, u); return null; }
            return await r.Content.ReadAsByteArrayAsync(c);
        }

        var listed = new Dictionary<string, Discovered>();
        await foreach (var d in source.DiscoverAsync(Fetch, ct)) listed.TryAdd(d.Url, d);
        var now = clock.GetUtcNow();
        var known = await db.Homes.Where(h => h.Source == source.SourceId).ToDictionaryAsync(h => h.Url ?? h.Id, ct);
        int fetched = 0, stored = 0, unchanged = 0, skipped = 0, blocked = 0, failures = 0;

        foreach (var d in listed.Values)
        {
            if (known.TryGetValue(d.Url, out var h) && d.LastModified is { } lm && lm <= h.CheckedAt)
            {
                h.LastSeenAt = now; h.GoneAt = null; unchanged++;
                continue;
            }
            if (fetched >= max) { skipped++; continue; }
            var uri = new Uri(d.Url);
            if (!robots.Allows(uri)) { blocked++; continue; }
            byte[]? bytes;
            try { bytes = await Fetch(uri, ct); failures = 0; }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                log.LogWarning("Couldn't fetch {Url}: {Error}", d.Url, ex.Message);
                if (++failures >= 5) throw new InvalidOperationException($"{source.Site.Host}: five failures in a row; stopping this run.", ex);
                continue;
            }
            finally { fetched++; }
            if (bytes is null) continue;
            Home? parsed;
            try { parsed = source.Parse(d.Url, System.Text.Encoding.UTF8.GetString(bytes)); }
            catch (JsonException ex) { log.LogWarning("Couldn't read {Url}: {Error}", d.Url, ex.Message); continue; }
            if (parsed is null) continue;
            if (h is null)
            {
                h = await db.Homes.FindAsync([parsed.Id], ct);
                if (h is null) { db.Homes.Add(h = parsed); h.FirstSeenAt = now; }
                known[d.Url] = h;
            }
            Copy(parsed, h);
            h.LastSeenAt = now; h.CheckedAt = now; h.GoneAt = null;
            stored++;
            if (stored % 25 == 0) { await db.SaveChangesAsync(ct); log.LogInformation("{Stored} homes stored so far", stored); }
        }

        var gone = 0;
        foreach (var h in known.Values.Where(h => h.GoneAt is null && h.Url is not null && !listed.ContainsKey(h.Url)))
        {
            h.GoneAt = now; gone++;
        }
        await db.SaveChangesAsync(ct);
        return new Result(listed.Count, fetched, stored, unchanged, skipped, gone, blocked);
    }

    static void Copy(Home from, Home to)
    {
        to.Source = from.Source; to.Agency = from.Agency; to.Url = from.Url; to.Price = from.Price; to.M2 = from.M2;
        to.Beds = from.Beds; to.Baths = from.Baths; to.Type = from.Type; to.Province = from.Province;
        to.Municipality = Clip(from.Municipality, 80); to.Postcode = Clip(from.Postcode, 8); to.District = Clip(from.District, 80);
        to.Area = from.Area; to.Features = from.Features; to.Energy = from.Energy;
    }

    async Task<string?> GetTextAsync(Uri u, CancellationToken ct)
    {
        using var r = await http.GetAsync(u, ct);
        return r.IsSuccessStatusCode ? await r.Content.ReadAsStringAsync(ct) : null;
    }

    /// <summary>The URLs and last-changed dates in a sitemap, gzipped or not.</summary>
    public static IEnumerable<Discovered> ReadSitemap(byte[] bytes)
    {
        Stream s = new MemoryStream(bytes);
        if (bytes.Length > 2 && bytes[0] == 0x1f && bytes[1] == 0x8b) s = new GZipStream(s, CompressionMode.Decompress);
        var doc = XDocument.Load(s);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        foreach (var u in doc.Descendants(ns + "url"))
        {
            var loc = u.Element(ns + "loc")?.Value.Trim();
            if (string.IsNullOrEmpty(loc)) continue;
            DateTimeOffset? lm = DateTimeOffset.TryParse(u.Element(ns + "lastmod")?.Value, out var t) ? t : null;
            yield return new Discovered(loc, lm);
        }
    }

    public static int? Int(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = Regex.Match(s, @"\d+([.,]\d+)?");
        return m.Success && double.TryParse(m.Value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? (int)Math.Round(v) : null;
    }

    public static string? Clip(string? s, int n) => s is null ? null : s.Length <= n ? s : s[..n];
}

/// <summary>Made-up homes for the PoC, so groups always see something (D-035). Fixed, not refreshed: they carry no link.
/// 90 in the Comunidad de Madrid, then 36 in other provinces (B-059), drawn separately so the first 90 stay as they were.</summary>
public static class DemoHomes
{
    public static IEnumerable<Home> Make(DateTimeOffset now)
    {
        var rnd = new Random(2026);
        var places = new (string Town, string Postcode, string Area)[]
        {
            ("Madrid", "28005", "centre"), ("Madrid", "28045", "centre"), ("Madrid", "28028", "centre"), ("Madrid", "28015", "centre"),
            ("Madrid", "28047", "city"), ("Madrid", "28041", "city"), ("Madrid", "28031", "city"), ("Madrid", "28033", "city"), ("Madrid", "28018", "city"),
            ("Las Rozas de Madrid", "28232", "north"), ("Alcobendas", "28100", "north"), ("Tres Cantos", "28760", "north"), ("Pozuelo de Alarcón", "28224", "north"),
            ("Getafe", "28901", "south"), ("Leganés", "28911", "south"), ("Alcorcón", "28921", "south"), ("Móstoles", "28931", "south"),
            ("Alcalá de Henares", "28801", "east"), ("Torrejón de Ardoz", "28850", "east"), ("Coslada", "28820", "east"),
        };
        var basePrice = new Dictionary<string, int> { ["centre"] = 6200, ["city"] = 3600, ["north"] = 3900, ["south"] = 2500, ["east"] = 2300 };
        for (var i = 1; i <= 90; i++)
        {
            var (town, pc, area) = places[rnd.Next(places.Length)];
            var house = area != "centre" && rnd.NextDouble() < 0.3;
            var beds = rnd.Next(2, house ? 7 : 6);
            var baths = Math.Max(1, Math.Min(beds - 1, rnd.Next(1, 4)));
            var m2 = beds * rnd.Next(18, 28) + 25 + (house ? 40 : 0);
            var feats = new List<string>();
            if (!house && rnd.NextDouble() < 0.7) feats.Add("lift");
            if (rnd.NextDouble() < (house ? 0.8 : 0.25)) feats.Add("parking");
            if (house ? rnd.NextDouble() < 0.85 : rnd.NextDouble() < 0.1) feats.Add("garden");
            if (rnd.NextDouble() < 0.4) feats.Add("terrace");
            if (rnd.NextDouble() < (house ? 0.4 : 0.15)) feats.Add("pool");
            if (rnd.NextDouble() < 0.35) feats.Add("storage");
            if (feats.Contains("lift") && rnd.NextDouble() < 0.5) feats.Add("stepfree");
            yield return new Home
            {
                Id = $"{HomeSources.Demo}:{i:000}",
                Source = HomeSources.Demo,
                Agency = "Inmobiliaria de ejemplo",
                Url = null,
                Price = (int)Math.Round(m2 * basePrice[area] * (0.85 + rnd.NextDouble() * 0.3) / 1000.0) * 1000,
                M2 = m2, Beds = beds, Baths = baths, Type = house ? "house" : "flat",
                Province = "Madrid", Municipality = town, Postcode = pc, Area = area,
                Features = string.Join(',', feats),
                Energy = "ABCDEFG"[rnd.Next(1, 7)].ToString(),
                FirstSeenAt = now, LastSeenAt = now, CheckedAt = now,
            };
        }
        // Elsewhere in Spain. Prices per m² are made up (Assumption), only roughly in line with each city
        var rnd2 = new Random(2059);
        var elsewhere = new (string Town, string Postcode, string Province, int PerM2)[]
        {
            ("Barcelona", "08013", "Barcelona", 4600), ("L'Hospitalet de Llobregat", "08901", "Barcelona", 3000), ("Sabadell", "08201", "Barcelona", 2400),
            ("València", "46010", "Valencia", 2500), ("Torrent", "46900", "Valencia", 1700),
            ("Sevilla", "41010", "Sevilla", 2400), ("Dos Hermanas", "41701", "Sevilla", 1600),
            ("Málaga", "29010", "Málaga", 3300), ("Bilbao", "48007", "Bizkaia", 3300),
            ("Zaragoza", "50009", "Zaragoza", 2100), ("Alicante", "03010", "Alicante", 2200), ("A Coruña", "15005", "A Coruña", 2300),
        };
        for (var i = 91; i <= 126; i++)
        {
            var (town, pc, province, perM2) = elsewhere[rnd2.Next(elsewhere.Length)];
            var house = rnd2.NextDouble() < 0.25;
            var beds = rnd2.Next(2, house ? 7 : 6);
            var baths = Math.Max(1, Math.Min(beds - 1, rnd2.Next(1, 4)));
            var m2 = beds * rnd2.Next(18, 28) + 25 + (house ? 40 : 0);
            var feats = new List<string>();
            if (!house && rnd2.NextDouble() < 0.7) feats.Add("lift");
            if (rnd2.NextDouble() < (house ? 0.8 : 0.25)) feats.Add("parking");
            if (house ? rnd2.NextDouble() < 0.85 : rnd2.NextDouble() < 0.1) feats.Add("garden");
            if (rnd2.NextDouble() < 0.4) feats.Add("terrace");
            if (rnd2.NextDouble() < (house ? 0.4 : 0.15)) feats.Add("pool");
            if (rnd2.NextDouble() < 0.35) feats.Add("storage");
            yield return new Home
            {
                Id = $"{HomeSources.Demo}:{i:000}",
                Source = HomeSources.Demo,
                Agency = "Inmobiliaria de ejemplo",
                Url = null,
                Price = (int)Math.Round(m2 * perM2 * (0.85 + rnd2.NextDouble() * 0.3) / 1000.0) * 1000,
                M2 = m2, Beds = beds, Baths = baths, Type = house ? "house" : "flat",
                Province = province, Municipality = town, Postcode = pc, Area = Regions.AreaFor(town, pc),
                Features = string.Join(',', feats),
                Energy = "ABCDEFG"[rnd2.Next(1, 7)].ToString(),
                FirstSeenAt = now, LastSeenAt = now, CheckedAt = now,
            };
        }
    }

    /// <summary>Made-up homes are on unless "Homes:Demo" is false. Off on the live site, where the crawler fills the index (F-019).</summary>
    public static bool Enabled(IConfiguration cfg) => !string.Equals(cfg["Homes:Demo"], "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>Adds any made-up homes that are missing if they're on; deletes them if they're off.</summary>
    public static async Task EnsureAsync(AppDb db, TimeProvider clock, IConfiguration cfg, CancellationToken ct = default)
    {
        if (!Enabled(cfg)) { await db.Homes.Where(h => h.Source == HomeSources.Demo).ExecuteDeleteAsync(ct); return; }
        var have = (await db.Homes.Where(h => h.Source == HomeSources.Demo).Select(h => h.Id).ToListAsync(ct)).ToHashSet();
        var missing = Make(clock.GetUtcNow()).Where(h => !have.Contains(h.Id)).ToList();
        if (missing.Count == 0) return;
        db.Homes.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }
}
