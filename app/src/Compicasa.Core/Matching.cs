namespace Compicasa.Core;

public sealed record HardResult(bool Ok, IReadOnlyList<Msg> Reasons, IReadOnlyList<Msg> Mine, IReadOnlyList<string> Theirs, IReadOnlyList<Msg> Both);
public sealed record ScoreResult(int Total, IReadOnlyDictionary<string, double> Parts, IReadOnlyList<string> Both, bool WfhClash);
public sealed record Explanation(ScoreResult Score, IReadOnlyList<Msg> Clicks, IReadOnlyList<Msg> Talks);
public sealed record CheckResult(bool Ok, IReadOnlyList<Msg> Issues);
public sealed record Pair(string A, string B, int Score);
public sealed record GroupScoreResult(int Min, int Avg, IReadOnlyList<Pair> Pairs, Pair? Weakest);
public sealed record SuggestedGroup(IReadOnlyList<string> Ids, int Min, int Avg, int Size);
public sealed record FeatureNeed(string Id, string Level, IReadOnlyList<string> Must, IReadOnlyList<string> Nice);
public sealed record Extra(Msg Label, Msg Why);
public sealed record RoomShare(string Id, int Rooms, bool Bath, int Pct);
public sealed record Topic(string Key, Msg? Why);
public sealed record HouseBrief(
    int Beds, int Baths, int PrivateBaths, int SharedBaths, int Cars, int Desks, int Kids, int Adults,
    IReadOnlyList<string> Areas, IReadOnlyList<string> Types, IReadOnlyList<FeatureNeed> Feats,
    IReadOnlyList<Extra> Extras, int M2, int M2Min, int M2Max, bool GuestWc);

/// <summary>
/// The v0.3 matching rules, ported one-for-one from the prototype's logic block
/// (prototype/compicasa-prototype.html, LOGIC START to LOGIC END). Pure functions, no I/O.
/// Wherever the prototype used the user ("you"), these take the viewer's id.
/// </summary>
public static class Matching
{
    static List<string> Inter(IEnumerable<string> a, IEnumerable<string> b) { var bs = b.ToList(); return a.Where(bs.Contains).ToList(); }

    /// <summary>JavaScript's Math.round for the non-negative values used here (ties go up).</summary>
    static int JsRound(double x) => (int)Math.Floor(x + 0.5);

    static string[] Ids(IEnumerable<Profile> ps) => ps.Select(p => p.Id).ToArray();

    public static bool SmokeAccepts(string acc, string s) => s == "no" || (s == "outside" && acc != "none") || (s == "inside" && acc == "any");
    public static bool PetsOk(bool acc, IReadOnlyCollection<string> pets) => pets.Count == 0 || acc;

    static int HIndex(Profile p) { var i = Array.IndexOf(Catalogue.Horizons, p.Horizon); return i < 0 ? 1 : i; }

    /// <summary>Everyone is buying to rent the home out (B-063): the living-together rules don't apply.</summary>
    public static bool Letting(IEnumerable<Profile> all) => all.All(p => p.IsLet());

    /// <summary>Dealbreakers, checked both ways from a's point of view. The other side's reasons stay private.
    /// People match only with the same purpose (B-063); two people buying to let skip the living-together rules.
    /// The household-type rule applies only between two made-up people: never to or by a real person (B-042).</summary>
    public static HardResult Hard(Profile a, Profile b)
    {
        if (a.Purpose != b.Purpose)
        {
            var diff = new Msg("purposeDiff", b.Purpose);
            return new HardResult(false, [diff], [], [], [diff]);
        }
        var mine = new List<Msg>(); var theirs = new List<string>(); var both = new List<Msg>();
        var letting = a.IsLet();
        if (!letting)
        {
            if (!a.Real && !b.Real)
            {
                if (!a.Open.Contains(b.Household)) mine.Add(new Msg("notOpen", b.Household));
                if (!b.Open.Contains(a.Household)) theirs.Add("household");
            }
            if (!SmokeAccepts(a.AcceptsSmoking, b.Smoking)) mine.Add(new Msg(b.Smoking == "inside" ? "smokeIn" : "smokeOut"));
            if (!SmokeAccepts(b.AcceptsSmoking, a.Smoking)) theirs.Add("smoking");
            if (!PetsOk(a.AcceptsPets, b.Pets)) mine.Add(new Msg("petsNo", b.Pets.ToArray()));
            if (!PetsOk(b.AcceptsPets, a.Pets)) theirs.Add("pets");
        }
        if (a.Areas.Count == 0) mine.Add(new Msg("pickArea"));
        else if (Inter(a.Areas, b.Areas).Count == 0) both.Add(new Msg("noArea", b.Areas.ToArray()));
        if (a.Types.Count == 0) mine.Add(new Msg("pickType"));
        else if (Inter(a.Types, b.Types).Count == 0) both.Add(new Msg("onlyType", b.Types.ToArray()));
        var beds = a.Bedrooms + b.Bedrooms;
        if (!letting && beds > Catalogue.MaxBedrooms) both.Add(new Msg("bedsPair", beds, Catalogue.MaxBedrooms));
        var reasons = mine.Concat(both).ToList();
        if (theirs.Count > 0) reasons.Add(new Msg("theirsPrivate"));
        return new HardResult(mine.Count == 0 && theirs.Count == 0 && both.Count == 0, reasons, mine, theirs, both);
    }

    /// <summary>Shared priorities. Accessibility needs are left out: they shape the brief, never the score.</summary>
    public static (double V, List<string> Both) HomeScore(Profile a, Profile b)
    {
        List<string> Keys(Profile x) => x.Needs.Keys.Where(f => !Catalogue.AccessFeatures.Contains(f)).ToList();
        var na = Keys(a); var nb = Keys(b);
        if (na.Count == 0 || nb.Count == 0) return (0.75, []);
        var both = na.Where(nb.Contains).ToList();
        double W(string f)
        {
            var am = a.Needs[f] == "must"; var bm = b.Needs[f] == "must";
            return am && bm ? 1 : (am || bm ? 0.75 : 0.5);
        }
        double s = 0; foreach (var f in both) s += W(f);
        return (0.75 + 0.25 * Math.Min(1, s / Math.Min(na.Count, nb.Count)), both);
    }

    /// <summary>Demo compatibility score, 0-100. Uncalibrated weights; household type is not scored.</summary>
    public static ScoreResult Score(Profile a, Profile b)
    {
        if (a.IsLet() && b.IsLet()) return LetScore(a, b);
        double life = 0;
        foreach (var (id, w) in Catalogue.Sliders) life += w * (1 - Math.Abs(a.Slider(id) - b.Slider(id)) / 4.0);
        var wfhClash = (a.Wfh && b.Social >= 4) || (b.Wfh && a.Social >= 4);
        if (wfhClash) life -= 0.08;
        life = Math.Clamp(life, 0, 1);
        var place = Place(a, b);
        var plans = Plans(a, b);
        var hs = HomeScore(a, b);
        var parts = new Dictionary<string, double> { ["lifestyle"] = life, ["place"] = place, ["plans"] = plans, ["home"] = hs.V };
        double tot = 0; foreach (var (k, w) in Catalogue.Weights) tot += w * parts[k];
        return new ScoreResult(JsRound(100 * tot), parts, hs.Both, wfhClash);
    }

    static double Place(Profile a, Profile b)
    {
        double ai = Inter(a.Areas, b.Areas).Count, ti = Inter(a.Types, b.Types).Count;
        var areaS = ai / Math.Max(1, Math.Min(a.Areas.Count, b.Areas.Count));
        var typeS = ti / Math.Max(1, Math.Min(a.Types.Count, b.Types.Count));
        return 0.7 * areaS + 0.3 * typeS;
    }

    static double Plans(Profile a, Profile b) => new[] { 1, 0.6, 0.2 }[Math.Abs(HIndex(a) - HIndex(b))];

    /// <summary>Two people buying to rent out (B-063): where and what, how long they'd keep it, shared priorities. No "how you live".</summary>
    static ScoreResult LetScore(Profile a, Profile b)
    {
        var hs = HomeScore(a, b);
        var parts = new Dictionary<string, double> { ["place"] = Place(a, b), ["plans"] = Plans(a, b), ["home"] = hs.V };
        double tot = 0; foreach (var (k, w) in Catalogue.LetWeights) tot += w * parts[k];
        return new ScoreResult(JsRound(100 * tot), parts, hs.Both, false);
    }

    /// <summary>Why you click and what to talk about, from u's point of view.</summary>
    public static Explanation Explain(Profile u, Profile p)
    {
        var sc = Score(u, p); var clicks = new List<Msg>(); var talks = new List<Msg>();
        if (u.IsLet() && p.IsLet())
        {
            // Buying to rent out: only plans and shared priorities are worth a word (B-063).
            var d = Math.Abs(HIndex(u) - HIndex(p));
            if (d == 0) clicks.Add(new Msg("sameHorizon", Catalogue.Horizons[HIndex(u)]));
            else if (d == 2) talks.Add(new Msg("diffHorizon", Catalogue.Horizons[HIndex(u)], Catalogue.Horizons[HIndex(p)]));
            foreach (var f in sc.Both.Take(2)) clicks.Add(new Msg("bothWant", f));
            return new Explanation(sc, clicks.Take(4).ToList(), talks.Take(4).ToList());
        }
        int D(string k) => Math.Abs(u.Slider(k) - p.Slider(k));
        if (p.Pets.Count > 0) talks.Add(new Msg("has", p.Pets.ToArray()));
        if (p.Smoking == "outside") talks.Add(new Msg("smokesOut"));
        if (p.Smoking == "inside") talks.Add(new Msg("smokesIn"));
        if (D("sleep") <= 1) clicks.Add(new Msg(u.Sleep <= 2 && p.Sleep <= 2 ? "bothLarks" : (u.Sleep >= 4 && p.Sleep >= 4 ? "bothOwls" : "similarRhythm")));
        else if (D("sleep") >= 3) talks.Add(new Msg(p.Sleep > u.Sleep ? "lateHours" : "veryEarly"));
        if (D("tidy") <= 1) clicks.Add(new Msg("similarTidy"));
        else if (D("tidy") >= 2) talks.Add(new Msg(p.Tidy > u.Tidy ? "tidier" : "relaxed"));
        if (u.Meals >= 4 && p.Meals >= 4) clicks.Add(new Msg("cookTogether"));
        else if (u.Meals <= 2 && p.Meals <= 2) clicks.Add(new Msg("ownMeals"));
        else if (D("meals") >= 3) talks.Add(new Msg("mealsDiffer"));
        if (D("guests") >= 3) talks.Add(new Msg(p.Guests > u.Guests ? "guestsOften" : "guestsRarely"));
        if (D("social") >= 3) talks.Add(new Msg(p.Social > u.Social ? "likesLively" : "likesQuiet"));
        if (u.Wfh && p.Social >= 4) talks.Add(new Msg("youWfh"));
        else if (p.Wfh && u.Social >= 4) talks.Add(new Msg("theyWfh"));
        var hd = Math.Abs(HIndex(u) - HIndex(p));
        if (hd == 0) clicks.Add(new Msg("sameHorizon", Catalogue.Horizons[HIndex(u)]));
        else if (hd == 2) talks.Add(new Msg("diffHorizon", Catalogue.Horizons[HIndex(u)], Catalogue.Horizons[HIndex(p)]));
        foreach (var f in sc.Both.Take(2)) clicks.Add(new Msg("bothWant", f));
        if (p.Kids > 0 && u.Kids == 0) talks.Add(new Msg("kidsInHome", p.Kids));
        return new Explanation(sc, clicks.Take(4).ToList(), talks.Take(4).ToList());
    }

    /// <summary>Would a accept b? No dealbreaker applies and the score meets a's pickiness.
    /// Used to simulate the made-up profiles' replies, and to estimate suggested groups.</summary>
    public static bool Accepts(Profile a, Profile b) => Hard(a, b).Ok && Score(a, b).Total >= a.Pickiness;

    /// <summary>The first pair among <paramref name="others"/> who wouldn't accept each other, if any.</summary>
    public static (Profile A, Profile B)? OthersReject(IReadOnlyList<Profile> others, Func<Profile, Profile, bool>? accepts = null)
    {
        accepts ??= Accepts;
        for (var i = 0; i < others.Count; i++)
            for (var j = i + 1; j < others.Count; j++)
                if (!accepts(others[i], others[j]) || !accepts(others[j], others[i])) return (others[i], others[j]);
        return null;
    }

    /// <summary>Whole-group checks. <paramref name="all"/>[0] is the viewer.</summary>
    public static CheckResult GroupCheck(IReadOnlyList<Profile> all)
    {
        var issues = new List<Msg>();
        var maxH = all.Min(m => m.MaxHouseholds);
        if (all.Count > maxH) issues.Add(new Msg("maxHH", maxH, Ids(all.Where(m => m.MaxHouseholds == maxH))));
        var beds = all.Sum(m => m.Bedrooms);
        if (!Letting(all) && beds > Catalogue.MaxBedrooms) issues.Add(new Msg("bedsGroup", beds, Catalogue.MaxBedrooms));
        for (var i = 0; i < all.Count; i++)
            for (var j = i + 1; j < all.Count; j++)
            {
                var h = Hard(all[i], all[j]);
                if (!h.Ok) issues.Add(i == 0 ? new Msg("pairReason", all[j].Id, h.Reasons[0]) : new Msg("clash", new[] { all[i].Id, all[j].Id }));
            }
        if (CommonAreas(all).Count == 0) issues.Add(new Msg("noAreaAll"));
        if (CommonTypes(all).Count == 0) issues.Add(new Msg("noTypeAll"));
        return new CheckResult(issues.Count == 0, issues);
    }

    public static List<string> CommonAreas(IEnumerable<Profile> all) => all.Aggregate(Catalogue.Areas.ToList(), (acc, m) => Inter(acc, m.Areas));
    public static List<string> CommonTypes(IEnumerable<Profile> all) => all.Aggregate(Catalogue.Types.ToList(), (acc, m) => Inter(acc, m.Types));

    public static GroupScoreResult GroupScore(IReadOnlyList<Profile> all)
    {
        var pairs = new List<Pair>();
        for (var i = 0; i < all.Count; i++)
            for (var j = i + 1; j < all.Count; j++)
                pairs.Add(new Pair(all[i].Id, all[j].Id, Score(all[i], all[j]).Total));
        if (pairs.Count == 0) return new GroupScoreResult(0, 0, pairs, null);
        var w = pairs[0];
        foreach (var p in pairs) if (p.Score < w.Score) w = p;
        return new GroupScoreResult(w.Score, JsRound(pairs.Sum(p => (double)p.Score) / pairs.Count), pairs, w);
    }

    static int RankGroups(SuggestedGroup x, SuggestedGroup y)
    {
        var c = y.Min - x.Min; if (c != 0) return c;
        c = y.Avg - x.Avg; if (c != 0) return c;
        return x.Size - y.Size;
    }

    /// <summary>Best whole group of each size from the viewer's mutual matches, best first.
    /// Everyone must pass the group checks and accept each other.</summary>
    public static IReadOnlyList<SuggestedGroup> BestGroups(Profile user, IReadOnlyList<Profile> mutualMatches, int limit = 3, Func<Profile, Profile, bool>? accepts = null)
    {
        var c = mutualMatches;
        var maxOthers = Math.Max(0, Math.Min(3, user.MaxHouseholds - 1));
        var outList = new List<SuggestedGroup>();
        void Consider(params Profile[] others)
        {
            var all = new List<Profile> { user }; all.AddRange(others);
            if (!GroupCheck(all).Ok || OthersReject(others, accepts) is not null) return;
            var gs = GroupScore(all);
            outList.Add(new SuggestedGroup(Ids(others), gs.Min, gs.Avg, all.Count));
        }
        for (var i = 0; i < c.Count; i++)
        {
            if (maxOthers >= 1) Consider(c[i]);
            for (var j = i + 1; j < c.Count; j++)
            {
                if (maxOthers >= 2) Consider(c[i], c[j]);
                for (var k = j + 1; k < c.Count; k++) if (maxOthers >= 3) Consider(c[i], c[j], c[k]);
            }
        }
        // Stable sort, like Array.prototype.sort in modern engines.
        var sorted = outList.Select((g, i) => (g, i)).OrderBy(x => x, Comparer<(SuggestedGroup g, int i)>.Create((a, b) => RankGroups(a.g, b.g) is var r && r != 0 ? r : a.i - b.i)).Select(x => x.g);
        var best = new List<SuggestedGroup>();
        foreach (var g in sorted) if (!best.Any(b => b.Size == g.Size)) best.Add(g);
        return best.Take(limit).ToList();
    }

    /// <summary>What the shared home needs.</summary>
    public static HouseBrief Brief(IReadOnlyList<Profile> all)
    {
        if (all.Count > 0 && Letting(all)) return LetBrief(all);
        var beds = all.Sum(m => m.Bedrooms);
        var privateBaths = all.Count(m => m.PrivateBath);
        var sharers = all.Where(m => !m.PrivateBath).ToList();
        var sharedBaths = sharers.Count > 0 ? Math.Max(1, (int)Math.Ceiling(sharers.Sum(m => m.Adults + m.Kids) / 4.0)) : 0;
        var baths = Math.Max(1, privateBaths + sharedBaths);
        var cars = all.Sum(m => m.Cars);
        var wfh = all.Where(m => m.Wfh).ToList();
        int kids = all.Sum(m => m.Kids), adults = all.Sum(m => m.Adults);
        var feats = Catalogue.Features.Select(f =>
        {
            var must = all.Where(m => m.Needs.GetValueOrDefault(f) == "must").ToList();
            var nice = all.Where(m => m.Needs.GetValueOrDefault(f) == "nice").ToList();
            return new FeatureNeed(f, must.Count > 0 ? "must" : (nice.Count > 0 ? "nice" : ""), Ids(must), Ids(nice));
        }).Where(x => x.Level != "").ToList();
        var extras = new List<Extra>();
        var second = all.Count >= 2 && kids > 0;
        if (second) extras.Add(new Extra(new Msg("secondLiving"), new Msg(all.Any(m => m.Kids == 0) ? "secondWhyKidless" : "secondWhy")));
        var avgMeals = all.Sum(m => (double)m.Meals) / all.Count;
        if (avgMeals >= 3.5) extras.Add(new Extra(new Msg("bigKitchen"), new Msg("bigKitchenWhy")));
        else if (all.Count >= 2) extras.Add(new Extra(new Msg("twoKitchen"), new Msg("twoKitchenWhy")));
        if (wfh.Count > 0) extras.Add(new Extra(new Msg("desks", wfh.Count), new Msg("wfhWho", Ids(wfh))));
        var dogOwners = all.Where(m => m.Pets.Contains("dog")).ToList();
        if (dogOwners.Count > 0) extras.Add(new Extra(new Msg("outdoor"), new Msg("dogWho", Ids(dogOwners))));
        var guestWc = privateBaths == all.Count && all.Count >= 2;
        if (guestWc) extras.Add(new Extra(new Msg("guestWC"), new Msg("guestWCWhy")));
        var est = (beds * 12 + baths * 5 + 30 + 12 + wfh.Count * 5 + (second ? 12 : 0) + (guestWc ? 3 : 0)) * 1.15;
        int R5(double x) => JsRound(x / 5) * 5;
        return new HouseBrief(beds, baths, privateBaths, sharedBaths, cars, wfh.Count, kids, adults,
            CommonAreas(all), CommonTypes(all), feats, extras, R5(est), R5(est * 0.9), R5(est * 1.1), guestWc);
    }

    /// <summary>
    /// A home to rent out (B-063): the home itself, not who sleeps where. Each "bedrooms" answer means "at least", so the
    /// home needs the most anyone asked for; one bathroom at least (Assumption); no parking, desks, kids or extras.
    /// Size uses the same formula: (bedrooms x 12 + bathrooms x 5 + 42) x 1.15, plus or minus 10%.
    /// </summary>
    static HouseBrief LetBrief(IReadOnlyList<Profile> all)
    {
        var beds = all.Max(m => m.Bedrooms);
        const int baths = 1;
        var feats = Catalogue.Features.Select(f =>
        {
            var must = all.Where(m => m.Needs.GetValueOrDefault(f) == "must").ToList();
            var nice = all.Where(m => m.Needs.GetValueOrDefault(f) == "nice").ToList();
            return new FeatureNeed(f, must.Count > 0 ? "must" : (nice.Count > 0 ? "nice" : ""), Ids(must), Ids(nice));
        }).Where(x => x.Level != "").ToList();
        var est = (beds * 12 + baths * 5 + 30 + 12) * 1.15;
        int R5(double x) => JsRound(x / 5) * 5;
        return new HouseBrief(beds, baths, 0, 0, 0, 0, 0, all.Sum(m => m.Adults),
            CommonAreas(all), CommonTypes(all), feats, [], R5(est), R5(est * 0.9), R5(est * 1.1), false);
    }

    public static IReadOnlyList<RoomShare> RoomPlan(IReadOnlyList<Profile> all)
    {
        if (all.Count > 0 && Letting(all)) return []; // nobody lives there (B-063)
        var beds = all.Sum(m => m.Bedrooms); if (beds == 0) beds = 1;
        return all.Select(m => new RoomShare(m.Id, m.Bedrooms, m.PrivateBath, JsRound(100.0 * m.Bedrooms / beds))).ToList();
    }

    /// <summary>Room placement tips. <paramref name="viewerId"/> gets "your rooms" wording.</summary>
    public static IReadOnlyList<Msg> PlacementTips(IReadOnlyList<Profile> all, string viewerId)
    {
        var r = new List<Msg>();
        if (all.Count > 0 && Letting(all)) return r; // nobody lives there (B-063)
        var multi = all.Where(m => m.Bedrooms >= 2).ToList();
        if (all.Count >= 2 && multi.Count == 1) r.Add(multi[0].Id == viewerId ? new Msg("keepYours") : new Msg("keepTheirs", multi[0].Id));
        else if (all.Count >= 2 && multi.Count > 1) r.Add(new Msg("clusters", Ids(multi)));
        var owls = all.Where(m => m.Sleep >= 4).ToList(); var larks = all.Where(m => m.Sleep <= 2).ToList();
        if (owls.Count > 0 && larks.Count > 0) r.Add(new Msg("owlsLarks", Ids(owls), Ids(larks)));
        var wfh = all.Where(m => m.Wfh).ToList();
        if (wfh.Count > 0 && all.Any(m => m.Kids > 0)) r.Add(new Msg("wfhAway", wfh.Count > 1));
        var en = all.Where(m => m.PrivateBath).ToList();
        if (en.Count > 0 && en.Count < all.Count) r.Add(new Msg("ensuite", Ids(en)));
        if (r.Count == 0) r.Add(new Msg("similarNeeds"));
        return r;
    }

    /// <summary>House agreement starters. A null Why means the topic's default reason.</summary>
    public static IReadOnlyList<Topic> AgreementTopics(IReadOnlyList<Profile> all)
    {
        var r = new List<Topic>();
        if (all.Count > 0 && Letting(all))
        {
            // Buying to rent out (B-063): prompts for a conversation and the lawyer, never advice.
            r.Add(new Topic("agLetKind", null));
            r.Add(new Topic("agLetManage", null));
            r.Add(new Topic("agLetMoney", null));
            r.Add(new Topic("agDecisions", null));
            r.Add(new Topic("agExit", null));
            return r;
        }
        int Rng(string k) => all.Max(m => m.Slider(k)) - all.Min(m => m.Slider(k));
        if (Rng("tidy") >= 2) r.Add(new Topic("agCleaning", null));
        if (Rng("guests") >= 3) r.Add(new Topic("agGuests", null));
        if (Rng("sleep") >= 3) r.Add(new Topic("agHours", null));
        var wfh = all.Where(m => m.Wfh).ToList();
        if (Rng("social") >= 3 || wfh.Count > 0) r.Add(new Topic("agQuiet", wfh.Count > 0 ? new Msg("wfhWho", Ids(wfh)) : null));
        if (Rng("meals") >= 3) r.Add(new Topic("agFood", null));
        var petOwners = all.Where(m => m.Pets.Count > 0).ToList();
        if (petOwners.Count > 0) r.Add(new Topic("agPets", new Msg("petWho", Ids(petOwners))));
        var smokers = all.Where(m => m.Smoking != "no").ToList();
        if (smokers.Count > 0) r.Add(new Topic("agSmoking", new Msg("smokerWho", Ids(smokers))));
        if (all.Any(m => m.Kids > 0)) r.Add(new Topic("agKids", null));
        var cars = all.Sum(m => m.Cars);
        if (cars >= 2) r.Add(new Topic("agParking", new Msg("carsTotal", cars)));
        if (all.Any(m => m.Needs.ContainsKey("garden") || m.Needs.ContainsKey("pool"))) r.Add(new Topic("agGarden", null));
        r.Add(new Topic("agDecisions", null));
        r.Add(new Topic("agExit", null));
        return r;
    }
}
