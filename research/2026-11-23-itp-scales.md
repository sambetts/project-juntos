# Regional ITP scales for a resale home (B-060)

(Research, with the Builder.) 2026-11-23, Session 26. Follows the general rates in `product/2026-11-23-all-of-spain.md` (Session 24, D-045).

## Summary
- Seven regions charge more for dearer resale homes: Aragón, Asturias, Illes Balears, Castilla y León, Cataluña, Comunitat Valenciana and Extremadura. All seven scales are **Verified** in each region's consolidated law on BOE, checked 2026-11-23. They match what Session 24 summarised.
- The scales work in two ways. Aragón, Illes Balears, Castilla y León, Cataluña and Extremadura tax each slice of the value at its own band's rate. Asturias and the Comunitat Valenciana tax the whole value at the rate of the band it falls in, so the tax jumps at 300,000 €, 500,000 € and 1 million €.
- The agreed-home estimate now applies each region's scale to the whole home's price, then splits the tax by share (`itpTax` in `app/src/Compicasa.Api/wwwroot/i18n.js`). A home we can't place uses the most any region charges at that price.
- For co-buyers, the base matters. Asturias says outright that its rate follows the whole property's value even when only a share is bought, and Illes Balears and Cataluña set the rate from the property's total value. Castilla y León and Extremadura apply theirs to the taxable base, which might be each buyer's share. If so, co-buyers there could pay less than the estimate. That's a question for the lawyer (below).
- Many reduced rates exist for a main home (young buyers, large families, disability, protected housing). Several turn on each buyer's own circumstances or share, which co-buyers may meet differently. They're listed below as open questions and aren't shown to users as advice.

## Findings
All are labelled **Verified**: the article was read in BOE's consolidated text on 2026-11-23. "Updated" is the last update date shown on that BOE page.

| Region | Bands (value of the home) | How it applies | Source | Label |
|---|---|---|---|---|
| Aragón | 8% to 400,000 €; 8.5% to 450,000 €; 9% to 500,000 €; 9.5% to 750,000 €; 10% above | Each slice at its rate; the law gives the running total (32,000 €, 36,250 €, 40,750 €, 64,500 €). The average rate this gives is then applied to the taxable base "in the same proportion as to the value of the property" | [DL 1/2005, art. 121-1](https://www.boe.es/buscar/act.php?id=BOA-d-2005-90006) (art. as amended by Ley 10/2015; page updated 07/07/2025) | Verified |
| Asturias | 8% to 300,000 €; 9% from 300,000.01 € to 500,000 €; 10% above | The whole value at one rate, set by "the full value of the property or right, whether or not the transfer covers all of it" | [DL 2/2014, art. 26](https://www.boe.es/buscar/act.php?id=BOE-A-2015-945) (updated 31/12/2025) | Verified |
| Illes Balears | 8% to 400,000 €; 9% to 600,000 €; 10% to 1,000,000 €; 12% to 2,000,000 €; 13% above | An average rate: the scale's tax on the total value divided by that value, rounded to two decimals (the second decimal goes up only when the third is above 5). That rate is then applied to each buyer's taxable base | [DL 1/2014, art. 10.a](https://www.boe.es/buscar/act.php?id=BOE-A-2014-6925) (updated 13/06/2026) | Verified |
| Castilla y León | 8% to 250,000 €; 10% on the part above | Slices: "the general rate on this amount and 10% on the part of the base above it" | [DL 1/2013, arts. 24.1.a and 25.1](https://www.boe.es/buscar/act.php?id=BOCL-h-2013-90254) (updated 14/05/2024) | Verified (see open questions on later changes) |
| Cataluña | 10% to 600,000 €; 11% to 900,000 €; 12% to 1,500,000 €; 13% above | An average rate from the scale on the home's total value (running totals 60,000 €, 93,000 €, 165,000 €). 20% for large holders (art. 641-1.5), which doesn't apply to individual co-buyers | [DL 1/2024, art. 641-1.1](https://www.boe.es/buscar/act.php?id=BOE-A-2024-6951) (updated 13/07/2026) | Verified |
| Comunitat Valenciana | 9%; 11% when the value is above 1,000,000 € | The whole value at 11% above 1 million | [Ley 13/1997, art. 13.Uno](https://www.boe.es/buscar/act.php?id=BOE-A-1998-8202) (updated 10/08/2026) | Verified |
| Extremadura | 8% to 360,000 €; 10% from 360,000.01 € to 600,000 €; 11% above | Slices: "the sum of the amounts in each step, each at its own rate", applied to the taxable base | [DL 1/2018, art. 36](https://www.boe.es/buscar/act.php?id=BOE-A-2018-8159) (updated 04/08/2026) | Verified |

Checked by tool (`app/tests/itp/itp-check.mjs`, 77 checks, all passed). The expected amounts were worked out by hand from the tables above. Examples, at a price of 800,000 €:

| Region | Tax | Average rate |
|---|---|---|
| Aragón | 69,500 € | 8.69% |
| Asturias | 80,000 € | 10% |
| Illes Balears | 70,000 € | 8.75% |
| Castilla y León | 75,000 € | 9.38% |
| Cataluña | 82,000 € | 10.25% |
| Comunitat Valenciana | 72,000 € | 9% |
| Extremadura | 74,800 € | 9.35% |

Two details from the texts:
- **Illes Balears:** the "rest of value up to" column of the 1,000,000 € row reads 2,000,000 €. The next row starts at 2,000,000.01 € with a running total of 210,000 €, which is 90,000 € plus 12% of 1,000,000 €, so the band is 1–2 million €. The code uses that.
- **Cataluña:** the law sets an average rate but, unlike Illes Balears, gives no rounding rule, so the estimate uses the exact figure.

Where the code applies them: each region in `REGIONS` (`i18n.js`) has `itp`, the first-band rate, and, for these seven, `scale: {by: 'part' | 'whole', bands: [[up to €, rate %], …], avg2}`. `itpTax(price, region)` returns the tax and average rate, and `itpFor` in `app.js` feeds that rate into the per-person table. The note under the table still says it's an estimate, not advice. It now says the rate is "for a resale home at this price".

## Reduced rates that co-buyers might meet (not used; never advice)
From the same texts, article titles and opening wording only. Conditions weren't analysed.
- **Aragón:** reductions on the tax for a main home: art. 121-4 (under 35, disability, victims of gender violence; home up to 100,000 €) and art. 121-5 (large families).
- **Asturias:** 3% for protected housing as a main home (art. 27). Other reductions weren't checked.
- **Illes Balears:** 4% for a first main home up to 270,151.20 € when the buyer takes at least 50% and owns no other home at 50% or more. 2% for under-36s and other groups (art. 10.c–d; more in art. 17).
- **Castilla y León:** 4% for a main home for large families, disability of 65% or more, protected housing, or when *all* buyers are under 36 (art. 25.3). 0.01% for under-36s with further conditions (art. 25.4).
- **Cataluña:** reduced rates for a main home for large families, single-parent families, disability and young buyers (arts. 641-2 to 641-5). 7% for protected housing (art. 641-1.2).
- **Comunitat Valenciana:** 8%, 6%, 4% and 3% for young buyers under 35 "for the part they acquire", protected housing, large or single-parent families, disability and victims of gender violence, most with value and income limits (art. 13.Dos–Cinco).
- **Extremadura:** 7% for a main home up to 200,000 € with income limits (art. 40). 4% for under-36s, large families and disability with the same limits (art. 41). 4% for protected housing (art. 39).

## Open questions
- **The base for co-buyers in Castilla y León and Extremadura.** Is the scale applied to the whole home's value or to each buyer's share? Their wording ("base imponible", "base liquidable") could mean the share, which would lower the tax for co-buyers. The estimate assumes the whole value, so it errs high. Add to the lawyer brief (B-022 or its successor).
- **Reduced rates for a share.** Several reduced rates depend on each buyer's age, family, income or share (Illes Balears needs a 50% share; Castilla y León's under-36 rate needs *all* buyers under 36; Valencia applies "for the part they acquire"). The lawyer should say how they apply to mixed groups. Users only see "some buyers pay less".
- **Castilla y León changes after May 2024.** BOE's consolidated text was last updated on 14/05/2024 and cites changes up to Ley 4/2024. I found no later change to art. 25: I searched BOE and the web, and guiafiscal.es (2026, Reported) gives the same 8%/10% at 250,000 €. The Junta's own tax site and the Ministry of Finance's Tributación Autonómica 2026 weren't reached (the Ministry's site timed out again, as in Session 24).
- **The 2026 Ministry summary:** worth one more try as a cross-check of all seventeen regions when it's reachable.

## Sources (all checked 2026-11-23)
- BOE consolidated texts linked in the table, read as HTML from boe.es.
- guiafiscal.es, Castilla y León ITP 2026, https://guiafiscal.es/patrimonio/itp/castilla-y-leon/ (Reported; search result summary only).
- Not reached: hacienda.gob.es (Tributación Autonómica 2026 PDF and the online law compilation; connection timed out).
