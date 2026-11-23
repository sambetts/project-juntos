using System.Text.Json;

namespace Compicasa.Core;

/// <summary>
/// The 14 made-up Madrid profiles from the prototype (seed-profiles.json, exported by
/// tests/parity/export-from-prototype.js). They are demo people: their replies are simulated.
/// </summary>
public static class Seeds
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static readonly Lazy<IReadOnlyList<Profile>> All = new(() =>
    {
        using var s = typeof(Seeds).Assembly.GetManifestResourceStream("Compicasa.Core.seed-profiles.json")
            ?? throw new InvalidOperationException("seed-profiles.json is missing from the assembly");
        var list = JsonSerializer.Deserialize<List<Profile>>(s, Json)!;
        foreach (var p in list) p.MaxBudget ??= Budgets.DemoFor(p); // the prototype has no budgets (B-058)
        return list;
    });

    /// <summary>Fresh copies, so callers can't change the shared list.</summary>
    public static IReadOnlyList<Profile> Profiles() => All.Value.Select(p => p.Clone()).ToList();
}
