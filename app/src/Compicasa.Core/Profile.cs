namespace Compicasa.Core;

/// <summary>Text in both product languages (D-024). Real users write one text; it fills both.</summary>
public sealed record LocalizedText(string Es, string En);

/// <summary>
/// A household's profile, as in the prototype's P() defaults. Some fields are private to their owner:
/// who they'd share with, what they accept, their household cap and pickiness (see <see cref="PublicProfile"/>).
/// </summary>
public sealed class Profile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Household { get; set; } = "single";
    public int Adults { get; set; } = 1;
    public int Kids { get; set; }
    public int Bedrooms { get; set; } = 1;
    public bool PrivateBath { get; set; }
    public int Cars { get; set; }
    public List<string> Pets { get; set; } = [];
    public bool AcceptsPets { get; set; } = true;
    public string Smoking { get; set; } = "no";
    public string AcceptsSmoking { get; set; } = "outside";
    public int Sleep { get; set; } = 3;
    public int Tidy { get; set; } = 3;
    public int Social { get; set; } = 3;
    public int Guests { get; set; } = 3;
    public int Meals { get; set; } = 3;
    public bool Wfh { get; set; }
    public string Horizon { get; set; } = "5-9";
    public List<string> Areas { get; set; } = ["centre", "city"];
    public List<string> Types { get; set; } = ["flat", "house"];
    public Dictionary<string, string> Needs { get; set; } = [];
    public List<string> Open { get; set; } = ["single", "couple", "family", "singleParent"];
    public int MaxHouseholds { get; set; } = 3;
    public int Pickiness { get; set; } = 60;
    public string Color { get; set; } = "#495057";
    public LocalizedText? Bio { get; set; }

    public int Slider(string id) => id switch
    {
        "sleep" => Sleep,
        "tidy" => Tidy,
        "social" => Social,
        "guests" => Guests,
        "meals" => Meals,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
    };

    public Profile Clone()
    {
        var c = (Profile)MemberwiseClone();
        c.Pets = [.. Pets];
        c.Areas = [.. Areas];
        c.Types = [.. Types];
        c.Needs = new Dictionary<string, string>(Needs);
        c.Open = [.. Open];
        return c;
    }
}

/// <summary>What other people may see of a profile: no dealbreaker settings, sliders or pickiness.</summary>
public sealed record PublicProfile(
    string Id, string Name, string Household, int Adults, int Kids, int Bedrooms, bool PrivateBath, int Cars,
    IReadOnlyList<string> Pets, string Smoking, bool Wfh, string Horizon, IReadOnlyList<string> Areas,
    IReadOnlyList<string> Types, IReadOnlyDictionary<string, string> Needs, string Color, LocalizedText? Bio, bool IsDemo)
{
    public static PublicProfile From(Profile p, bool isDemo) => new(
        p.Id, p.Name, p.Household, p.Adults, p.Kids, p.Bedrooms, p.PrivateBath, p.Cars, p.Pets, p.Smoking, p.Wfh,
        p.Horizon, p.Areas, p.Types, p.Needs, p.Color, p.Bio, isDemo);
}
