using System.Globalization;
using System.Security.Claims;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>A home someone saved while browsing (B-064), before or alongside any group. Private to them.</summary>
public sealed class SavedHome
{
    public string AccountId { get; set; } = "";
    public string HomeId { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

/// <summary>
/// The house browser (B-064, Sam 2026-11-24): every home in the index for signed-in people, filtered and paged on the
/// server (about 3,000 homes is too much to send to a phone), each scored against the viewer's own profile with the
/// same rules as a group's homes. Prices are never compared with anyone's budget here: a person's budget is their share,
/// not a home's price (D-044).
/// </summary>
public static class Browse
{
    public const int PageMax = 48, SavedMax = 200;
    public static readonly string[] Sorts = ["fit", "priceUp", "priceDown", "pm2", "size", "new"];
    const string Energies = "ABCDEFG";

    /// <summary><c>Mine</c>: only the areas in the viewer's profile; otherwise <c>Areas</c> (comma-separated ids), or all of Spain.</summary>
    public sealed record Query(bool Mine, string? Areas, string? Province, int? Pmin, int? Pmax, int? Beds, int? M2, string? Type,
        string? Energy, bool Fits, bool Saved, bool Real, string? Sort, int Skip, int Take);

    public static void Map(WebApplication app)
    {
        var me = app.MapGroup("/api").RequireAuthorization();

        me.MapGet("/browse", async (HttpContext http, AppDb db, IConfiguration cfg, TimeProvider clock, bool? mine, string? areas, string? province, int? pmin, int? pmax,
            int? beds, int? m2, string? type, string? energy, bool? fits, bool? saved, bool? real, string? sort, int? skip, int? take) =>
        {
            var a = await OnboardedAsync(http, db); if (a is null) return Results.Unauthorized();
            var q = new Query(mine == true, areas, province, pmin, pmax, beds, m2, type, energy, fits == true, saved == true, real == true, sort, skip ?? 0, take ?? 24);
            return Results.Ok(await SearchAsync(db, cfg, clock, a, q, http.RequestAborted));
        });

        me.MapPut("/saved/{*homeId}", async (string homeId, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            var a = await OnboardedAsync(http, db); if (a is null) return Results.Unauthorized();
            if (!await db.Homes.AnyAsync(h => h.Id == homeId && h.GoneAt == null, http.RequestAborted)) return Results.Json(new { error = "unknownHome" }, statusCode: 404);
            if (await db.SavedHomes.FindAsync([a.Id, homeId], http.RequestAborted) is null)
            {
                if (await db.SavedHomes.CountAsync(s => s.AccountId == a.Id, http.RequestAborted) >= SavedMax) return Results.Json(new { error = "savedFull" }, statusCode: 409);
                db.SavedHomes.Add(new SavedHome { AccountId = a.Id, HomeId = homeId, At = clock.GetUtcNow() });
                await db.SaveChangesAsync(http.RequestAborted);
            }
            return Results.NoContent();
        });

        me.MapDelete("/saved/{*homeId}", async (string homeId, HttpContext http, AppDb db) =>
        {
            var a = await OnboardedAsync(http, db); if (a is null) return Results.Unauthorized();
            await db.SavedHomes.Where(s => s.AccountId == a.Id && s.HomeId == homeId).ExecuteDeleteAsync(http.RequestAborted);
            return Results.NoContent();
        });
    }

    static async Task<Account?> OnboardedAsync(HttpContext http, AppDb db)
    {
        var id = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is null ? null : await db.Accounts.FirstOrDefaultAsync(x => x.Id == id && x.Kind == AccountKind.User && x.OnboardedAt != null, http.RequestAborted);
    }

    static double? Pm2(Home h) => h.Price is int p && h.M2 is int m && m > 0 ? (double)p / m : null;

    static double? Median(IEnumerable<double> values)
    {
        var s = values.OrderBy(x => x).ToList();
        return s.Count == 0 ? null : s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    static bool EnergyAtLeast(string? have, string? want) =>
        string.IsNullOrEmpty(want) || (have is { Length: 1 } && Energies.Contains(have[0]) && Energies.IndexOf(have[0]) <= Energies.IndexOf(want[0]));

    public static async Task<object> SearchAsync(AppDb db, IConfiguration cfg, TimeProvider clock, Account a, Query q, CancellationToken ct)
    {
        var hb = Matching.Brief([a.ReadProfile()]);
        // From the in-memory index (HomeIndex): about 3,000 homes, not read from the table on every filter change.
        var index = await HomeIndex.LiveAsync(db, cfg, clock, ct);
        var live = index.Select(e => e.Home).ToList();
        var listing = index.ToDictionary(e => e.Home.Id, e => e.Listing);
        var saved = (await db.SavedHomes.Where(s => s.AccountId == a.Id).Select(s => s.HomeId).ToListAsync(ct)).ToHashSet();
        var areaSet = q.Mine ? hb.Areas.ToHashSet() : (q.Areas ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        var sel = live
            .Where(h => (areaSet.Count == 0 || (h.Area is not null && areaSet.Contains(h.Area)))
                && (string.IsNullOrEmpty(q.Province) || h.Province == q.Province)
                && (q.Pmin is null || h.Price >= q.Pmin) && (q.Pmax is null || h.Price <= q.Pmax)
                && (q.Beds is null || (h.Beds ?? 0) >= q.Beds) && (q.M2 is null || (h.M2 ?? 0) >= q.M2)
                && (string.IsNullOrEmpty(q.Type) || h.Type == q.Type)
                && EnergyAtLeast(h.Energy, q.Energy)
                && (!q.Saved || saved.Contains(h.Id)) && (!q.Real || !h.IsDemo))
            .Select(h => (H: h, Fit: HomeFit.Fit(hb, hb.M2Min, hb.M2Max, listing[h.Id])))
            .Where(x => !q.Fits || x.Fit.Ok)
            .ToList();

        var ordered = (Sorts.Contains(q.Sort) ? q.Sort : "fit") switch
        {
            "priceUp" => sel.OrderBy(x => x.H.Price ?? int.MaxValue),
            "priceDown" => sel.OrderByDescending(x => x.H.Price ?? 0),
            "pm2" => sel.OrderBy(x => Pm2(x.H) ?? double.MaxValue),
            "size" => sel.OrderByDescending(x => x.H.M2 ?? 0),
            "new" => sel.OrderByDescending(x => x.H.FirstSeenAt.UtcDateTime),
            _ => sel.OrderByDescending(x => x.Fit.Ok).ThenBy(x => x.H.IsDemo).ThenByDescending(x => x.Fit.Score).ThenBy(x => x.H.Price ?? int.MaxValue),
        };
        var page = ordered.ThenBy(x => x.H.Id, StringComparer.Ordinal).Skip(Math.Max(0, q.Skip)).Take(Math.Clamp(q.Take, 1, PageMax)).ToList();
        var es = StringComparer.Create(new CultureInfo("es-ES"), true);

        return new
        {
            considered = live.Count,
            total = sel.Count,
            fitting = sel.Count(x => x.Fit.Ok),
            savedCount = saved.Count,
            medPrice = Median(sel.Where(x => x.H.Price is not null).Select(x => (double)x.H.Price!.Value)),
            medPm2 = Median(sel.Select(x => Pm2(x.H)).OfType<double>()),
            myAreas = hb.Areas,
            areas = live.Where(h => h.Area is not null).GroupBy(h => h.Area!).Select(g => new { id = g.Key, n = g.Count() }).OrderBy(x => x.id, StringComparer.Ordinal).ToList(),
            provinces = live.Where(h => !string.IsNullOrEmpty(h.Province)).GroupBy(h => h.Province!).Select(g => new { name = g.Key, n = g.Count() }).OrderBy(x => x.name, es).ToList(),
            homes = page.Select(x => new
            {
                x.H.Id, x.H.Agency, Url = x.H.IsDemo ? null : x.H.Url, x.H.Price, x.H.M2, x.H.Beds, x.H.Baths, x.H.Type,
                x.H.Municipality, x.H.District, x.H.Province, x.H.Area, Features = x.H.Features.Split(',', StringSplitOptions.RemoveEmptyEntries), x.H.Energy, x.H.IsDemo, x.H.Photo,
                x.Fit.Score, x.Fit.Ok, x.Fit.Fits, x.Fit.Misses, x.Fit.Check, saved = saved.Contains(x.H.Id),
            }).ToList(),
        };
    }
}
