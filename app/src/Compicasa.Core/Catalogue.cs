namespace Compicasa.Core;

/// <summary>
/// The fixed option lists from the prototype's v0.3 model (product/profile-and-matching-model.md).
/// Only ids live here: every label users see is in the front end's string tables (D-024, D-028).
/// </summary>
public static class Catalogue
{
    public const int MaxBedrooms = 6;

    public static readonly string[] Areas = ["centre", "city", "north", "south", "east"];
    public static readonly string[] Types = ["flat", "house"];
    public static readonly string[] Households = ["single", "couple", "family", "singleParent"];
    public static readonly string[] Features = ["parking", "garden", "terrace", "pool", "lift", "stepfree", "schools", "transport", "quiet", "storage"];
    public static readonly string[] Horizons = ["2-4", "5-9", "10+"];
    public static readonly string[] Pets = ["dog", "cat"];
    public static readonly string[] Smoking = ["no", "outside", "inside"];
    public static readonly string[] AcceptsSmoking = ["none", "outside", "any"];

    /// <summary>Accessibility needs shape the home brief and never the score.</summary>
    public static readonly string[] AccessFeatures = ["lift", "stepfree"];

    public static readonly (string Id, double Weight)[] Sliders =
    [
        ("sleep", 0.25), ("tidy", 0.25), ("social", 0.20), ("guests", 0.15), ("meals", 0.15),
    ];

    /// <summary>Uncalibrated first guess (demo score). Household type is never scored.</summary>
    public static readonly (string Part, double Weight)[] Weights =
    [
        ("lifestyle", 0.50), ("place", 0.20), ("plans", 0.20), ("home", 0.10),
    ];
}
