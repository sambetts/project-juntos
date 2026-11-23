# All of Spain (B-059, D-045)

(Product, with the Builder.) 2026-11-23, Session 24.

## Summary
- Sam asked for it on 2026-11-23: "Do it. We need to support all of Spain". Until now areas, the crawl and the cost estimate were Madrid only.
- People choose where they would buy from 56 areas: Madrid's five (kept, so existing profiles and the pilot plan, D-008, are unchanged) and every other province.
- Homes are placed by postcode. The first two digits of a Spanish postcode are the province's INE code (Reported: Correos' numbering, not rechecked this session).
- The Tecnocasa crawl keeps homes for sale anywhere in Spain, with the same limit of 3,000 adverts a day, so the rest of Spain fills in over several days.
- The agreed-home estimate (D-043) uses each region's general ITP rate for a resale home, Verified on 2026-11-23 in each region's consolidated law (Gipuzkoa Reported). It uses the first band only, so it is low for dearer homes in regions with a scale.

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
**Verified** 2026-11-23 in each region's current consolidated tax law (Research, Session 24): BOE's consolidated texts, which include regional laws, and the provincial governments' own texts for Bizkaia and Álava. The Ministry of Finance's yearly summary (Tributación Autonómica 2026) wasn't reachable (connection refused from two networks). The estimate uses the rate in the first band; "Scale" shows how dearer homes are taxed. The first pass (secondary sources) had Galicia at 9% and Murcia at 8%; both are corrected below.

| Region | Rate used | Scale for dearer homes | Source (consolidated text; last update shown there) |
|---|---|---|---|
| Andalucía | 7% | None | [Ley 5/2021, art. 41](https://www.boe.es/buscar/act.php?id=BOE-A-2021-17915) (31/12/2025) |
| Aragón | 8% | 8% to 400,000 €, then 8.5% to 10% on the rest | [DL 1/2005, art. 121-1](https://www.boe.es/buscar/act.php?id=BOA-d-2005-90006) (07/07/2025) |
| Asturias | 8% | 9% above 300,000 €, 10% above 500,000 €, on the whole value | [DL 2/2014, art. 26](https://www.boe.es/buscar/act.php?id=BOE-A-2015-945) (31/12/2025) |
| Illes Balears | 8% | Average rate: 8% to 400,000 €, then 9%, 10%, 12%, 13% | [DL 1/2014, art. 10](https://www.boe.es/buscar/act.php?id=BOE-A-2014-6925) (13/06/2026) |
| Canarias | 6.5% | None (5% for a main home up to 200,000 €) | [DL 1/2009, art. 31](https://www.boe.es/buscar/act.php?id=BOC-j-2009-90008) (29/12/2025) |
| Cantabria | 9% | None (reduced rates for a main home) | [DL 62/2008, art. 9](https://www.boe.es/buscar/act.php?id=BOCT-c-2008-90028) (30/04/2026) |
| Castilla y León | 8% | 10% on the part above 250,000 € | [DL 1/2013, arts. 24–25](https://www.boe.es/buscar/act.php?id=BOCL-h-2013-90254) (14/05/2024) |
| Castilla-La Mancha | 9% | None | [Ley 8/2013, art. 19](https://www.boe.es/buscar/act.php?id=BOE-A-2014-1368) (30/03/2026) |
| Cataluña | 10% | Average rate: 10% to 600,000 €, then 11%, 12%, 13%; 20% for large holders | [DL 1/2024, art. 641-1](https://www.boe.es/buscar/act.php?id=BOE-A-2024-6951) (13/07/2026) |
| Comunitat Valenciana | 9% | 11% above 1 million € | [Ley 13/1997, art. 13](https://www.boe.es/buscar/act.php?id=BOE-A-1998-8202) (10/08/2026) |
| Extremadura | 8% | 10% on 360,000–600,000 €, 11% above | [DL 1/2018, art. 36](https://www.boe.es/buscar/act.php?id=BOE-A-2018-8159) (04/08/2026) |
| Galicia | 8% | None | [DL 1/2011, art. 14](https://www.boe.es/buscar/act.php?id=BOE-A-2011-18161) (27/03/2026) |
| Comunidad de Madrid | 6% | None | [DL 1/2010, art. 28](https://www.boe.es/buscar/act.php?id=BOCM-m-2010-90068) (10/07/2026) |
| Región de Murcia | 7.75% | None | [DL 1/2010, art. 6](https://www.boe.es/buscar/act.php?id=BOE-A-2011-10542) (24/07/2025) |
| Navarra | 6% | None | [DFL 129/1999, art. 8](https://www.boe.es/buscar/act.php?id=BON-n-1999-90001) (20/04/2026) |
| La Rioja | 7% | None | [Ley 10/2017, art. 44](https://www.boe.es/buscar/act.php?id=BOE-A-2017-13750) (29/10/2026) |
| País Vasco: Bizkaia | 4% | Homes, for a buyer with no more than 5 homes; 7% for other property | [NF 1/2011, art. 13](https://www.bizkaia.eus/documents/880307/15187815/ca_1_2011.pdf), Diputación Foral (20/04/2026) |
| País Vasco: Álava | 4% | Same condition, otherwise 6%; 7% for other property | [NF 11/2003, art. 43](https://web.araba.eus/documents/d/araba/indice_norma-foral-itp-ajd-cas-2-pdf), Diputación Foral (to DNUF 2/2026) |
| País Vasco: Gipuzkoa | 4% | Reported only: no current official consolidated text found (gipuzkoa.eus has a 2011 text; guiafiscal.es gives 4%) | NF 18/1987 |
| Ceuta, Melilla | 6% | The state's default rate where a region has set none | [RDL 1/1993, art. 11.1.a](https://www.boe.es/buscar/act.php?id=BOE-A-1993-25359) (21/03/2026). Art. 45.I.B.14 keeps the 1955 law's reductions for Ceuta and Melilla, often reported as 50%; not checked, so the estimate may be too high there |
| Home we can't place | 10% | | So the estimate isn't too low |

## Open questions
- Use each region's full scale in the estimate (proposed: B-060).
- Gipuzkoa's current text, and the 1955 reductions for Ceuta and Melilla.
- Finer areas inside other big cities (Barcelona, Valencia, Sevilla), as Madrid has: wait for real geography (B-043).
