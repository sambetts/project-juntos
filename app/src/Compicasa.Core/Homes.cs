using System.Globalization;
using System.Text;

namespace Compicasa.Core;

/// <summary>
/// A home for sale, as our index holds it (D-035): facts and the link to the agency's own advert only.
/// No photos, description text or agents' contact details. Made-up homes have no link.
/// </summary>
public sealed record HomeListing(
    string Id, string Source, string Agency, string? Url, int? Price, int? M2, int? Beds, int? Baths,
    string? Type, string? Municipality, string? Postcode, string? District, string? Area,
    IReadOnlyList<string> Features, string? Energy, bool IsDemo);

/// <summary>How a home fits a group's brief. Codes, not sentences: the front end words them (D-024).</summary>
public sealed record HomeFitResult(bool Ok, int Score, IReadOnlyList<string> Fits, IReadOnlyList<string> Misses, IReadOnlyList<string> Check);

public static class HomeFit
{
    /// <summary>Features an advert can tell us about. The rest (schools, transport, quiet) are for the group to check.</summary>
    public static readonly string[] Knowable = ["parking", "garden", "terrace", "pool", "lift", "stepfree", "storage"];

    /// <summary>
    /// Hard rules: enough bedrooms, a home type and an area the group accepts. Then a score out of 100:
    /// bathrooms, size against the group's m² range, must-haves the advert confirms, nice-to-haves as a bonus.
    /// Price isn't used until the group has a price band (B-043).
    /// </summary>
    public static HomeFitResult Fit(HouseBrief b, int m2Min, int m2Max, HomeListing h)
    {
        var fits = new List<string>();
        var misses = new List<string>();
        var check = new List<string>();
        var ok = true;

        if (h.Beds is int beds && beds >= b.Beds) fits.Add("beds");
        else { ok = false; misses.Add("beds"); }
        if (b.Types.Count > 0 && (h.Type is null || !b.Types.Contains(h.Type))) { ok = false; misses.Add("type"); }
        if (b.Areas.Count > 0 && (h.Area is null || !b.Areas.Contains(h.Area))) { ok = false; misses.Add("area"); }
        else if (h.Area is not null) fits.Add("area");

        var score = 100;
        if (h.Baths is int baths)
        {
            if (baths >= b.Baths) fits.Add("baths");
            else { misses.Add("baths"); score -= 15 * (b.Baths - baths); }
        }
        else { check.Add("baths"); score -= 5; }

        if (h.M2 is int m2)
        {
            if (m2 < m2Min) { misses.Add("small"); score -= Math.Min(30, (int)Math.Ceiling(100.0 * (m2Min - m2) / m2Min)); }
            else if (m2 > m2Max * 1.25) { misses.Add("big"); score -= 5; }
            else fits.Add("size");
        }
        else { check.Add("size"); score -= 10; }

        var wanted = b.Feats.ToList();
        if (b.Cars > 0 && !wanted.Any(f => f.Id == "parking")) wanted.Add(new FeatureNeed("parking", "must", [], []));
        foreach (var f in wanted)
        {
            if (!Knowable.Contains(f.Id)) { if (f.Level == "must") check.Add("f:" + f.Id); continue; }
            var has = h.Features.Contains(f.Id);
            if (has) { fits.Add("f:" + f.Id); if (f.Level == "nice") score += 3; }
            else if (f.Level == "must") { misses.Add("f:" + f.Id); score -= 12; }
        }
        return new HomeFitResult(ok, Math.Clamp(score, 0, 100), fits, misses, check);
    }

    /// <summary>The homes that pass the hard rules: real ones first, each best first.</summary>
    public static IReadOnlyList<(HomeListing Home, HomeFitResult Fit)> Rank(HouseBrief b, int m2Min, int m2Max, IEnumerable<HomeListing> homes, int limit) =>
        homes.Select(h => (Home: h, Fit: Fit(b, m2Min, m2Max, h)))
            .Where(x => x.Fit.Ok)
            .OrderBy(x => x.Home.IsDemo) // real homes always first; made-up ones only fill the gaps
            .ThenByDescending(x => x.Fit.Score)
            .ThenBy(x => x.Home.Price ?? int.MaxValue)
            .ThenBy(x => x.Home.Id, StringComparer.Ordinal)
            .Take(limit).ToList();
}

/// <summary>
/// Places a home in the app's five Madrid areas (Catalogue.Areas) from its town and postcode.
/// Approximate (Assumption, Session 15): "inside the M-30" is judged by postcode, and some postcodes straddle it.
/// Real geography replaces this with B-043.
/// </summary>
public static class MadridZones
{
    static readonly HashSet<string> Central =
    [
        "28001", "28002", "28003", "28004", "28005", "28006", "28007", "28008", "28009", "28010", "28012", "28013",
        "28014", "28015", "28016", "28020", "28028", "28036", "28039", "28045", "28046",
    ];

    static readonly Dictionary<string, string> Towns = Build(new()
    {
        ["north"] = ["las rozas de madrid", "las rozas", "pozuelo de alarcon", "alcobendas", "tres cantos", "san sebastian de los reyes",
            "majadahonda", "boadilla del monte", "torrelodones", "collado villalba", "colmenar viejo", "villanueva de la canada",
            "villaviciosa de odon", "galapagar", "algete"],
        ["south"] = ["getafe", "leganes", "alcorcon", "mostoles", "fuenlabrada", "parla", "pinto", "valdemoro", "arroyomolinos",
            "humanes de madrid", "navalcarnero", "ciempozuelos"],
        ["east"] = ["alcala de henares", "torrejon de ardoz", "coslada", "san fernando de henares", "rivas-vaciamadrid", "rivas vaciamadrid",
            "mejorada del campo", "arganda del rey", "velilla de san antonio", "meco", "paracuellos de jarama", "loeches"],
    });

    static Dictionary<string, string> Build(Dictionary<string, string[]> m) =>
        m.SelectMany(kv => kv.Value.Select(t => (t, kv.Key))).ToDictionary(x => x.t, x => x.Key);

    public static string? For(string? municipality, string? postcode)
    {
        var town = Plain(municipality);
        if (town == "madrid") return postcode is not null && Central.Contains(postcode.Trim()) ? "centre" : "city";
        return Towns.GetValueOrDefault(town);
    }

    /// <summary>Lower case, no accents, single spaces.</summary>
    public static string Plain(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d) if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
