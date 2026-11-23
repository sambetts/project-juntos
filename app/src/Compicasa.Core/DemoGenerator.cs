namespace Compicasa.Core;

/// <summary>
/// Makes a day's batch of made-up profiles (D-029). Deterministic for a given date, so a refresh
/// can be repeated safely. First names only, drawn from common Spanish names and combined at random:
/// they describe no real person. Bios are assembled from bilingual fragments (D-024).
/// </summary>
public static class DemoGenerator
{
    public const int BatchSize = 30;

    /// <summary>Barcelona, Valencia, Sevilla, Málaga, Bizkaia, Zaragoza, Alicante (area ids: Regions).</summary>
    static readonly string[] OtherCities = ["p08", "p46", "p41", "p29", "p48", "p50", "p03"];

    static readonly string[] Names =
    [
        "Alba", "Adrián", "Alejandro", "Alicia", "Álvaro", "Andrea", "Ángela", "Antonio", "Beatriz", "Blanca", "Carlos", "Carla",
        "Claudia", "Cristina", "Daniel", "David", "Eva", "Fernando", "Gabriel", "Gonzalo", "Hugo", "Inés", "Isabel", "Jaime",
        "Jorge", "Julia", "Leire", "Lola", "Lorena", "Luis", "Manuel", "María", "Mario", "Marina", "Miguel", "Mónica", "Noelia",
        "Óscar", "Patricia", "Paula", "Pedro", "Rocío", "Rubén", "Samuel", "Sara", "Sergio", "Silvia", "Teresa", "Víctor", "Yolanda",
    ];

    static readonly string[] Colors =
    [
        "#b3370e", "#0f766e", "#7048e8", "#c2255c", "#1971c2", "#2a7a38", "#a85500", "#5f3dc4", "#0b7285", "#a61e4d",
        "#364fc7", "#087f5b", "#9c36b5", "#495057",
    ];

    static readonly LocalizedText[] Jobs =
    [
        new("Trabajo en una farmacia del barrio.", "I work at a local pharmacy."),
        new("Soy profesora de secundaria.", "I'm a secondary-school teacher."),
        new("Programo para una empresa de logística.", "I write software for a logistics firm."),
        new("Trabajo en un hospital, con turnos rotativos.", "I work rotating shifts at a hospital."),
        new("Llevo la contabilidad de varias pymes.", "I keep the books for a few small businesses."),
        new("Soy fisioterapeuta.", "I'm a physiotherapist."),
        new("Trabajo en atención al cliente, en remoto.", "I work in customer support, remotely."),
        new("Soy técnico de mantenimiento.", "I'm a maintenance technician."),
        new("Diseño interiores por mi cuenta.", "I'm a freelance interior designer."),
        new("Trabajo en una ONG.", "I work for a charity."),
        new("Soy cocinero en un restaurante del centro.", "I'm a cook at a restaurant in the centre."),
        new("Investigo en la universidad.", "I'm a university researcher."),
    ];

    static readonly LocalizedText[] Habits =
    [
        new("Me gusta correr por el Retiro los domingos.", "I like running in the Retiro on Sundays."),
        new("Toco el piano, con cascos si hace falta.", "I play the piano, with headphones if needed."),
        new("Cocino mucho y comparto lo que sobra.", "I cook a lot and share the leftovers."),
        new("Leo, hago puzles y me acuesto pronto.", "I read, do puzzles and go to bed early."),
        new("Salgo en bici casi todos los días.", "I'm out on my bike most days."),
        new("Cuido muchas plantas.", "I look after a lot of plants."),
        new("Viajo bastante por trabajo.", "I travel quite a lot for work."),
        new("Los fines de semana hago senderismo en la sierra.", "At weekends I go hiking in the sierra."),
    ];

    static readonly LocalizedText[] Wishes =
    [
        new("Busco una casa para muchos años.", "I'm looking for a home for the long run."),
        new("Quiero dejar de pagar alquiler.", "I want to stop paying rent."),
        new("Me gustaría tener algo de espacio exterior.", "I'd like some outdoor space."),
        new("Necesito estar cerca del transporte.", "I need to be near public transport."),
        new("Prefiero una casa tranquila y ordenada.", "I prefer a calm, tidy home."),
        new("Me encantaría compartir cenas de vez en cuando.", "I'd love to share dinners now and then."),
    ];

    /// <summary>What a made-up person buying to rent out says instead of a wish about living there (B-063).</summary>
    static readonly LocalizedText[] LetWishes =
    [
        new("Busco una vivienda para alquilarla a largo plazo.", "I'm looking for a home to let long term."),
        new("Quiero invertir en un piso para alquilar con más gente.", "I want to buy a flat to let, together with others."),
        new("Me interesa comprar para alquilar, no para vivir.", "I'm interested in buying to let, not to live in."),
    ];

    /// <summary>The id prefix for a day's batch; ids are "gen-YYYYMMDD-NN".</summary>
    public static string Prefix(DateOnly day) => $"gen-{day:yyyyMMdd}-";

    public static IReadOnlyList<Profile> Batch(DateOnly day, int size = BatchSize)
    {
        var rnd = new Random(day.Year * 10000 + day.Month * 100 + day.Day);
        T Pick<T>(IReadOnlyList<T> xs) => xs[rnd.Next(xs.Count)];
        List<string> Some(IReadOnlyList<string> xs, int min)
        {
            var r = xs.Where(_ => rnd.NextDouble() < 0.5).ToList();
            foreach (var x in xs.OrderBy(_ => rnd.Next())) { if (r.Count >= min) break; if (!r.Contains(x)) r.Add(x); }
            return xs.Where(r.Contains).ToList(); // keep catalogue order
        }
        int Slider() => Math.Clamp((int)Math.Round(3 + (rnd.NextDouble() + rnd.NextDouble() - 1) * 2.5), 1, 5);

        var list = new List<Profile>();
        for (var i = 0; i < size; i++)
        {
            var roll = rnd.NextDouble();
            var household = roll < 0.45 ? "single" : roll < 0.75 ? "couple" : roll < 0.90 ? "family" : "singleParent";
            var p = new Profile
            {
                Id = Prefix(day) + (i + 1).ToString("00"),
                Household = household,
                Adults = household is "couple" or "family" ? 2 : 1,
                Kids = household is "family" or "singleParent" ? rnd.Next(1, 3) : 0,
                Color = Pick(Colors),
                Sleep = Slider(), Tidy = Slider(), Social = Slider(), Guests = Slider(), Meals = Slider(),
                Wfh = rnd.NextDouble() < 0.35,
                Horizon = Pick(Catalogue.Horizons),
                Pickiness = rnd.Next(55, 71),
                MaxHouseholds = rnd.Next(2, 5),
            };
            var first = Pick(Names);
            p.Name = household is "couple" or "family" ? $"{first} & {Pick(Names.Where(n => n != first).ToList())}" : first;
            p.Bedrooms = household switch { "family" => 2 + (p.Kids > 1 ? 1 : 0), "singleParent" => 1 + Math.Min(p.Kids, 2), "couple" => rnd.NextDouble() < 0.3 ? 2 : 1, _ => 1 };
            p.PrivateBath = household != "single" || rnd.NextDouble() < 0.4;
            p.Cars = household is "family" or "singleParent" ? rnd.Next(0, 3) : (rnd.NextDouble() < 0.3 ? 1 : 0);
            p.Pets = rnd.NextDouble() < 0.25 ? [Pick(Catalogue.Pets)] : [];
            p.AcceptsPets = p.Pets.Count > 0 || rnd.NextDouble() < 0.75;
            p.Smoking = rnd.NextDouble() < 0.15 ? "outside" : "no";
            p.AcceptsSmoking = p.Smoking != "no" ? "outside" : Pick(new[] { "none", "outside", "outside", "any" });
            // Most made-up people look in Madrid, where the pilot starts (D-008); about a quarter in one of Spain's other big cities (B-059)
            p.Areas = rnd.NextDouble() < 0.75 ? Some(MadridZones.Ids, 1) : [Pick(OtherCities)];
            p.Types = household is "family" ? (rnd.NextDouble() < 0.7 ? ["house"] : ["flat", "house"]) : Some(Catalogue.Types, 1);
            p.Open = household is "family" or "singleParent" ? Some(Catalogue.Households, 2) : Some(Catalogue.Households, 2);
            if (!p.Open.Contains(household)) p.Open = Catalogue.Households.Where(h => h == household || p.Open.Contains(h)).ToList();
            foreach (var f in Catalogue.Features)
            {
                var r = rnd.NextDouble();
                if (r < 0.12) p.Needs[f] = "must"; else if (r < 0.30) p.Needs[f] = "nice";
            }
            if (p.Kids > 0 && !p.Needs.ContainsKey("schools")) p.Needs["schools"] = "nice";
            var job = Pick(Jobs); var habit = Pick(Habits); var wish = Pick(Wishes);
            p.Bio = new LocalizedText($"{job.Es} {habit.Es} {wish.Es}", $"{job.En} {habit.En} {wish.En}");
            p.MaxBudget = Budgets.DemoFor(p); // from the id, so the day's other answers stay as they were
            // B-063: about one in four buys to rent out, also from the id; their bio says so instead of a wish about living there
            p.Purpose = Purposes.DemoFor(p.Id);
            if (p.IsLet())
            {
                var lw = LetWishes[i % LetWishes.Length];
                p.Bio = new LocalizedText($"{job.Es} {habit.Es} {lw.Es}", $"{job.En} {habit.En} {lw.En}");
            }
            list.Add(p);
        }
        return list;
    }
}
