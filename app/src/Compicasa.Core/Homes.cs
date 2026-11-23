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
    /// With a <paramref name="budget"/> (the members' maximum budgets added up, B-058), a home whose price is over it fails too.
    /// </summary>
    public static HomeFitResult Fit(HouseBrief b, int m2Min, int m2Max, HomeListing h, int? budget = null)
    {
        var fits = new List<string>();
        var misses = new List<string>();
        var check = new List<string>();
        var ok = true;

        if (budget is int max)
        {
            if (h.Price is not int price) check.Add("price");
            else if (price > max) { ok = false; misses.Add("price"); } // within it says nothing: every home shown is
        }

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
    public static IReadOnlyList<(HomeListing Home, HomeFitResult Fit)> Rank(HouseBrief b, int m2Min, int m2Max, IEnumerable<HomeListing> homes, int limit, int? budget = null) =>
        homes.Select(h => (Home: h, Fit: Fit(b, m2Min, m2Max, h, budget)))
            .Where(x => x.Fit.Ok)
            .OrderBy(x => x.Home.IsDemo) // real homes always first; made-up ones only fill the gaps
            .ThenByDescending(x => x.Fit.Score)
            .ThenBy(x => x.Home.Price ?? int.MaxValue)
            .ThenBy(x => x.Home.Id, StringComparer.Ordinal)
            .Take(limit).ToList();
}

/// <summary>
/// Maximum budgets (B-058, D-044). Each household says the most it would put towards a home's price; a group can afford
/// a home whose price is within the total. Budgets filter homes only: never use them to match, filter or rank people
/// (AGENTS.md section 4, D-009).
/// </summary>
public static class Budgets
{
    public const int Min = 10_000, Max = 5_000_000;

    /// <summary>The total, or null if anyone hasn't set one yet (then prices aren't filtered).</summary>
    public static int? Combined(IReadOnlyList<Profile> all) =>
        all.Count > 0 && all.All(p => p.MaxBudget is not null) ? all.Sum(p => p.MaxBudget!.Value) : null;

    /// <summary>A made-up person's budget: 80,000 euros a bedroom plus 50,000 to 170,000, steady for the same id, in steps of 5,000.
    /// Made up (Assumption), only so the demo filters by price.</summary>
    public static int DemoFor(Profile p)
    {
        var h = 2166136261u;
        foreach (var ch in p.Id) { h ^= ch; h *= 16777619u; }
        return p.Bedrooms * 80_000 + 50_000 + (int)(h % 25) * 5_000;
    }
}

/// <summary>
/// Places a home in the province of Madrid in one of the app's five Madrid areas from its town and postcode
/// (homes elsewhere in Spain are placed by province: Regions.AreaFor).
/// Approximate (Assumption, Session 15): "inside the M-30" is judged by postcode, and some postcodes straddle it.
/// Real geography replaces this with B-043.
/// </summary>
public static class MadridZones
{
    public static readonly string[] Ids = ["centre", "city", "north", "south", "east"];

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
