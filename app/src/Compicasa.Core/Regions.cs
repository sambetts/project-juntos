namespace Compicasa.Core;

/// <summary>
/// Spain's provinces (B-059, D-045). An area id is either one of Madrid's five finer areas (MadridZones, kept because the
/// pilot starts there: D-008) or "p" plus the province's two-digit INE code for every other province ("p08" is Barcelona).
/// The first two digits of a Spanish postcode are its province's INE code, so any home with a postcode can be placed.
/// Labels and regions live in the front end's string tables (i18n.js, D-024).
/// </summary>
public static class Regions
{
    public const string MadridCode = "28";

    /// <summary>INE province codes 01 to 52 (50 provinces, Ceuta and Melilla).</summary>
    public static readonly string[] ProvinceCodes = [.. Enumerable.Range(1, 52).Select(i => i.ToString("00"))];

    /// <summary>Madrid's five areas, then every other province as "pNN".</summary>
    public static readonly string[] Areas =
        [.. MadridZones.Ids, .. ProvinceCodes.Where(c => c != MadridCode).Select(c => "p" + c)];

    /// <summary>The province code from a postcode ("08013" gives "08"), or null if it isn't a Spanish postcode.</summary>
    public static string? ProvinceOf(string? postcode)
    {
        var p = postcode?.Trim();
        if (p is null || p.Length != 5 || !p.All(char.IsAsciiDigit)) return null;
        var n = int.Parse(p[..2]);
        return n is >= 1 and <= 52 ? p[..2] : null;
    }

    /// <summary>
    /// The area a home is in: one of Madrid's five areas inside the province of Madrid (null for a Madrid town not on
    /// MadridZones' list), otherwise its province.
    /// </summary>
    public static string? AreaFor(string? municipality, string? postcode)
    {
        var code = ProvinceOf(postcode);
        if (code is null) return MadridZones.For(municipality, postcode); // no usable postcode: Madrid towns by name only
        return code == MadridCode ? MadridZones.For(municipality, postcode) : "p" + code;
    }
}
