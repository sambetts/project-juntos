# All of Spain (B-059, D-045)

(Product, with the Builder.) 2026-11-23, Session 24.

## Summary
- Sam asked for it on 2026-11-23: "Do it. We need to support all of Spain". Until now areas, the crawl and the cost estimate were Madrid only.
- People choose where they would buy from 56 areas: Madrid's five (kept, so existing profiles and the pilot plan, D-008, are unchanged) and every other province.
- Homes are placed by postcode. The first two digits of a Spanish postcode are the province's INE code (Reported: Correos' numbering, not rechecked this session).
- The Tecnocasa crawl keeps homes for sale anywhere in Spain, with the same limit of 3,000 adverts a day, so the rest of Spain fills in over several days.
- The agreed-home estimate (D-043) uses each region's general ITP rate for a resale home. The rates are Reported, from secondary sources, apart from Madrid's. Confirm each on the region's own site before real users rely on it.

## How it works
| Part | What | Where |
|---|---|---|
| Area ids | `centre`, `city`, `north`, `south`, `east` (Madrid), then `p01` to `p52` without `p28` | `app/src/Compicasa.Core/Regions.cs`, `Catalogue.Areas` |
| Placing a home | Postcode to province; inside Madrid, `MadridZones` as before (a Madrid town not on its list stays unplaced) | `Regions.AreaFor` |
| Picker | One folding group per region (Comunidad de Madrid first, then A–Z); groups with a choice open; a summary line and a count per region update as you tick | `areaPicker` in `wwwroot/app.js`; `REGIONS` in `wwwroot/i18n.js` |
| Crawl | The advert URL may name any province; shops and rentals are still skipped | `TecnocasaSource` in `app/src/Compicasa.Api/Homes.cs` |
| Made-up data | 36 homes in 12 towns outside Madrid (ids `demo:091` to `demo:126`, added to existing databases); about a quarter of the daily made-up people look in one of seven big cities | `DemoHomes`, `DemoGenerator` |
| Matching | Unchanged: shared areas count as before, so people in different provinces share no area | `Matching.cs` |

## ITP rates used (general rate, resale home)
Reported: secondary sources checked 2026-11-23 ([visorfiscal.es](https://visorfiscal.es/blog/itp-vivienda-usada-comunidades-autonomas), [bm.consulting](https://bm.consulting/es/insights/tipos-itp-comunidad-autonoma-2026/), [acfinan.com](https://www.acfinan.com/actualidad/itp-comunidades-autonomas-2026), [hipotecanow.es](https://hipotecanow.es/itp/)). Many regions charge more above a price threshold; the estimate uses the first band.

| Region | Rate | Note |
|---|---|---|
| País Vasco | 4% | Homes; 7% for other property |
| Comunidad de Madrid | 6% | Verified on the Comunidad's site (Session 22) |
| Navarra | 6% | |
| Canarias | 6.5% | |
| Andalucía, La Rioja | 7% | |
| Aragón, Asturias, Illes Balears, Castilla y León, Extremadura | 8% | Higher bands for dearer homes |
| Región de Murcia | 8% | Sources say 7.75% or 8%; the higher is used |
| Cantabria, Castilla-La Mancha | 9% | One source gives 10% for Cantabria |
| Galicia | 9% | Sources say 8% or 9%; the higher is used |
| Comunitat Valenciana | 9% | 11% above 1 million € |
| Cataluña | 10% | Higher bands up to 13% |
| Ceuta, Melilla | 6% | Assumption: not found in these sources |
| Home we can't place | 10% | So the estimate isn't too low |

## Open questions
- Confirm every rate on the region's own site or official bulletin before real users rely on the estimate.
- Finer areas inside other big cities (Barcelona, Valencia, Sevilla), as Madrid has: wait for real geography (B-043).
- Ceuta and Melilla have tax rules of their own; the 6% is a placeholder.
