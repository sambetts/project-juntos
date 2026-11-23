using Compicasa.Api;
using Compicasa.Core;

namespace Compicasa.Tests;

/// <summary>All of Spain (B-059, D-045): areas, placing homes by postcode, the crawl beyond Madrid. Made-up data only.</summary>
public class RegionsTests
{
    [Fact]
    public void Areas_are_Madrids_five_then_every_other_province()
    {
        Assert.Equal(5 + 51, Catalogue.Areas.Length);
        Assert.Equal(["centre", "city", "north", "south", "east"], Catalogue.Areas.Take(5));
        Assert.Contains("p08", Catalogue.Areas);
        Assert.Contains("p52", Catalogue.Areas);
        Assert.DoesNotContain("p28", Catalogue.Areas); // Madrid is its five areas
        Assert.Equal(Catalogue.Areas.Length, Catalogue.Areas.Distinct().Count());
        Assert.All(Catalogue.Areas, a => Assert.True(a.Length <= 12)); // Homes.Area column
    }

    [Theory]
    [InlineData("08013", "08")]
    [InlineData(" 46010 ", "46")]
    [InlineData("01001", "01")]
    [InlineData("52001", "52")]
    [InlineData("53001", null)]
    [InlineData("00123", null)]
    [InlineData("0801", null)]
    [InlineData("AB123", null)]
    [InlineData(null, null)]
    public void A_postcode_gives_its_province(string? postcode, string? code) => Assert.Equal(code, Regions.ProvinceOf(postcode));

    [Fact]
    public void Homes_are_placed_by_province_and_in_Madrid_by_its_five_areas()
    {
        Assert.Equal("p08", Regions.AreaFor("Barcelona", "08013"));
        Assert.Equal("p41", Regions.AreaFor("Dos Hermanas", "41701"));
        Assert.Equal("p35", Regions.AreaFor("Las Palmas de Gran Canaria", "35001"));
        Assert.Equal("centre", Regions.AreaFor("Madrid", "28005"));
        Assert.Equal("south", Regions.AreaFor("Getafe", "28901"));
        Assert.Null(Regions.AreaFor("Villanueva De Perales", "28609")); // a Madrid town not on the list, as before
        Assert.Equal("north", Regions.AreaFor("Las Rozas", null));
        Assert.Null(Regions.AreaFor("Sevilla", null));
    }

    [Fact]
    public void Profiles_may_choose_any_province_but_not_unknown_ones()
    {
        var p = Seeds.Profiles()[0];
        p.Areas = ["p46", "centre", "p08"];
        var (clean, problem) = ProfileRules.Clean(p, p);
        Assert.Null(problem);
        Assert.Equal(["centre", "p08", "p46"], clean!.Areas); // catalogue order
        p.Areas = ["p28"];
        Assert.Equal("badList", ProfileRules.Clean(p, p).Problem);
        p.Areas = ["p53"];
        Assert.Equal("badList", ProfileRules.Clean(p, p).Problem);
    }

    [Fact]
    public void A_group_in_Barcelona_sees_Barcelona_homes_and_people_in_different_provinces_share_no_area()
    {
        HouseBrief Brief(params string[] areas) => new(2, 1, 0, 1, 0, 0, 0, 2, areas, ["flat"], [], [], 70, 60, 90, false);
        HomeListing Home(string town, string pc) => new("d:1", "demo", "Demo", null, 200000, 70, 2, 1, "flat", town, pc, null, Regions.AreaFor(town, pc), [], "E", true);
        Assert.True(HomeFit.Fit(Brief("p08"), 60, 90, Home("Sabadell", "08201")).Ok);
        Assert.False(HomeFit.Fit(Brief("p08"), 60, 90, Home("Getafe", "28901")).Ok);
        Assert.True(HomeFit.Fit(Brief("p08", "south"), 60, 90, Home("Getafe", "28901")).Ok);

        var a = Seeds.Profiles()[0]; a.Areas = ["p08"];
        var b = Seeds.Profiles()[1]; b.Areas = ["p46"];
        Assert.Empty(Matching.CommonAreas([a, b]));
        b.Areas = ["p46", "p08"];
        Assert.Equal(["p08"], Matching.CommonAreas([a, b]));
    }

    [Fact]
    public async Task Tecnocasa_adverts_anywhere_in_Spain_are_kept_and_placed()
    {
        var html = HomesTests.AdvertPage(11, town: "Barcelona", postcode: "08013");
        var h = new TecnocasaSource().Parse("https://www.tecnocasa.es/venta/piso/barcelona/barcelona/11.html", html)!;
        Assert.Equal("p08", h.Area);

        string[] urls =
        [
            "https://www.tecnocasa.es/venta/piso/barcelona/barcelona/11.html",
            "https://www.tecnocasa.es/venta/chalet/santa-cruz-de-tenerife/la-laguna/12.html",
            "https://www.tecnocasa.es/venta/piso/madrid/getafe/13.html",
            "https://www.tecnocasa.es/venta/local-comercial/valencia/valencia/14.html",
            "https://www.tecnocasa.es/alquiler/piso/sevilla/sevilla/15.html",
        ];
        var xml = "<?xml version=\"1.0\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"
            + string.Concat(urls.Select(u => $"<url><loc>{u}</loc><lastmod>2026-11-21T00:00:00+01:00</lastmod></url>")) + "</urlset>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        var found = new List<string>();
        await foreach (var d in new TecnocasaSource().DiscoverAsync((u, _) => Task.FromResult<byte[]?>(u.AbsolutePath.EndsWith("-0.xml.gz") ? bytes : null), default))
            found.Add(d.Url);
        Assert.Equal(urls.Take(3), found); // homes for sale anywhere; never shops or rentals
    }

    [Fact]
    public void Made_up_homes_include_other_provinces()
    {
        var homes = DemoHomes.Make(DateTimeOffset.UnixEpoch).ToList();
        Assert.Equal(126, homes.Count);
        Assert.All(homes.Take(90), h => Assert.Equal("28", Regions.ProvinceOf(h.Postcode)));
        Assert.All(homes.Skip(90), h => Assert.NotEqual("28", Regions.ProvinceOf(h.Postcode)));
        Assert.All(homes, h => Assert.Contains(h.Area!, Catalogue.Areas));
    }
}
