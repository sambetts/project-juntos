using Compicasa.Core;

namespace Compicasa.Api;

/// <summary>Server-side checks on what a user may save in their profile. Unknown values are rejected, not guessed.</summary>
public static class ProfileRules
{
    public static Profile Default(string id, string name) => new() { Id = id, Name = name, Color = "#1b2230" };

    public static (Profile? Profile, string? Problem) Clean(Profile input, Profile current)
    {
        static bool Subset(IEnumerable<string>? xs, string[] allowed) => xs is not null && xs.All(allowed.Contains);
        static List<string> Ordered(IEnumerable<string> xs, string[] allowed) => allowed.Where(xs.Contains).ToList();
        static bool Slider(int v) => v is >= 1 and <= 5;

        var name = (input.Name ?? "").Trim();
        if (name.Length is < 1 or > 40) return (null, "badName");
        if (!Catalogue.Households.Contains(input.Household)) return (null, "badHousehold");
        if (input.Bedrooms is < 1 or > 4 || input.Cars is < 0 or > 2 || input.MaxHouseholds is < 2 or > 4) return (null, "badNumber");
        if (!(Slider(input.Sleep) && Slider(input.Tidy) && Slider(input.Social) && Slider(input.Guests) && Slider(input.Meals))) return (null, "badNumber");
        if (!Catalogue.Smoking.Contains(input.Smoking) || !Catalogue.AcceptsSmoking.Contains(input.AcceptsSmoking)) return (null, "badSmoking");
        if (!Catalogue.Horizons.Contains(input.Horizon)) return (null, "badHorizon");
        if (!Subset(input.Pets, Catalogue.Pets) || !Subset(input.Areas, Catalogue.Areas) || !Subset(input.Types, Catalogue.Types) || !Subset(input.Open, Catalogue.Households))
            return (null, "badList");
        if (input.Needs is null || input.Needs.Any(n => !Catalogue.Features.Contains(n.Key) || n.Value is not ("must" or "nice"))) return (null, "badNeeds");
        var bio = input.Bio?.Es?.Trim() ?? "";
        if (bio.Length > 400) return (null, "badBio");
        if (input.MaxBudget is int mb && mb is < Budgets.Min or > Budgets.Max) return (null, "badBudget");
        if (!Purposes.All.Contains(input.Purpose)) return (null, "badPurpose");

        var p = new Profile
        {
            Id = current.Id,
            Name = name,
            Household = input.Household,
            // Adults follow the household; kids only for households with children (prototype rule).
            Adults = input.Household is "couple" or "family" ? 2 : 1,
            Kids = input.Household is "family" or "singleParent" ? Math.Clamp(input.Kids, 1, 5) : 0,
            Bedrooms = input.Bedrooms,
            PrivateBath = input.PrivateBath,
            Cars = input.Cars,
            Pets = Ordered(input.Pets!, Catalogue.Pets),
            AcceptsPets = input.AcceptsPets,
            Smoking = input.Smoking,
            AcceptsSmoking = input.AcceptsSmoking,
            Sleep = input.Sleep, Tidy = input.Tidy, Social = input.Social, Guests = input.Guests, Meals = input.Meals,
            Wfh = input.Wfh,
            Horizon = input.Horizon,
            Areas = Ordered(input.Areas!, Catalogue.Areas),
            Types = Ordered(input.Types!, Catalogue.Types),
            Needs = Catalogue.Features.Where(input.Needs.ContainsKey).ToDictionary(f => f, f => input.Needs[f]),
            Open = Ordered(input.Open!, Catalogue.Households),
            MaxHouseholds = input.MaxHouseholds,
            // Pickiness only drives the made-up people's simulated replies; real people answer for themselves.
            Pickiness = current.Pickiness,
            Color = current.Color,
            Bio = bio.Length == 0 ? null : new LocalizedText(bio, bio),
            MaxBudget = input.MaxBudget,
            // B-063: live in it or rent it out. For "let", the living-together answers are kept, unused, so switching back restores them.
            Purpose = input.Purpose,
        };
        return (p, null);
    }
}
