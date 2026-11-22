# Our own index of homes for sale: can we scrape agencies? (2026-11-22)

_(Research, with Legal & Compliance and the Builder; Session 15)_

**Why:** Sam asked for our own database of homes for sale in Spain, gathered by scraping agencies' websites daily until formal feeds exist, with common facts on each home and a link to the original advert, so groups see homes that fit their brief and are referred to the agency. This clashes with D-004 and the off-limits list in AGENTS.md section 4 (no scraping), so this note tests the idea before Sam decides (F-017). No listing page, listing sitemap or listing feed was fetched: only robots.txt files, legal notices and the BOE.

## Outcome (same day)
Sam chose to crawl agencies' own websites daily, never portals, with facts and a link only (F-017 option B, D-035): "We need to clearly link each house to the original agent… Unless they specifically issue a cease & desist, I think this is an acceptable route for now." Built in the PoC (B-046). A test crawl of Tecnocasa stored 65 homes in the province of Madrid. Its sitemaps list about 1,250 homes for sale there in the first of two files, and its advert pages carry their facts as JSON. Redpiso's schema.org markup gives price and town but no bedrooms, so it needs its own adapter. The lawyer questions are in the brief (A7).

## Summary
- **Every agency network we checked restricts this in its own terms.** Of nine networks, six ban automated extraction, re-use of the database, or deep links without written permission. Two (RE/MAX, Century 21) forbid reproducing content without permission, RE/MAX only for commercial purposes. One (Engel & Völkers) couldn't be read. None says yes. (Verified, legal notices read 2026-11-22.)
- **Spanish law backs them.** The database right lets a site's maker forbid extracting a substantial part, and also "repeated or systematic" extraction of small parts that hurts normal use of the database (LPI art. 133.1 and 133.2, Verified, BOE). A daily re-crawl of an agency's whole stock is exactly that. Price, size and rooms are facts, not copyright works, but the database right covers them anyway.
- **Technically it would be easy, which is not the issue.** Tecnocasa, Redpiso, Donpiso, Solvia, Aliseda, RE/MAX and Engel & Völkers all list sitemaps in robots.txt, and Tecnocasa's list two sitemaps of property pages. Redpiso's robots.txt even welcomes AI crawlers, including ClaudeBot, but its legal notice still requires written permission for any link other than to its home page. robots.txt isn't a licence.
- **Our pitch to agencies is strong**, because we send them buyers and don't sell ads to them, so asking for permission is likely to work. The routes are a Kyero XML feed from their CRM (Witei and others already export it hourly) or written permission to read their site.
- **Recommendation:** build the index and the matching now, filled with made-up homes. Seed real homes by hand (a few dozen, facts and link only) and switch on daily automated imports agency by agency, only where an agency has said yes. Don't run an automated crawl without permission until the lawyer has answered (new question for the brief, B-003).

## Findings: what agency sites allow
| Site | robots.txt (2026-11-22) | Legal notice on extraction and links |
|---|---|---|
| Idealista (portal) | 586 lines; listing folders mostly allowed for search engines | Terms ban scraping, manual copying and deep links without written permission (earlier note, `2026-11-18-homes-sourcing.md`) |
| Fotocasa (portal) | Blocks `/property/`, `/search/`, `/buscar/` for all | Not re-checked; no public API found earlier |
| Tecnocasa | Almost all open; property sitemaps listed | Bans "extracting or re-using all or a substantial part of the contents" without authorisation; links allowed without frames or false claims |
| Redpiso | Open; explicitly allows AI crawlers | Any hyperlink needs prior written authorisation, home page only |
| Donpiso | Open; listing pages allowed | Users indemnify Donpiso for "robots, spiders, crawlers" used to extract data; links need express written authorisation; home-page links only for private, non-commercial use |
| Aliseda | Open apart from accounts and filters | Bans "extraction and/or re-use", naming the database right; links banned unless authorised, home page only |
| Servihabitat | Blocks `/venta` and `/alquiler` | Links to any page but the home page banned unless authorised |
| Solvia | Open apart from `/api/` | Links only to the home page; "no frames, no deep-links" |
| RE/MAX | Blocks query strings | Bans reproducing content "for commercial purposes" without authorisation; nothing on links |
| Century 21 | Redirects | Bans reproducing or communicating content without written authorisation; nothing on links |
| Engel & Völkers | Open apart from search | Notice loads by script; not read |

Whether website terms bind someone who never ticked "I accept" is a question for the lawyer (Assumption that it's contested; not researched here). The database right applies either way.

## Findings: the law in one table
| Question | Answer | Label |
|---|---|---|
| Can an agency forbid copying the facts of its whole stock? | Yes, if it invested substantially in obtaining, verifying or presenting them (LPI art. 133.1) | Verified, BOE consolidated text |
| And a daily re-crawl of small parts? | Also forbidden if repeated and systematic and against normal use (art. 133.2) | Verified, BOE |
| Does that apply to a search engine that links back? | It can: *CV-Online v Melons* (CJEU C-762/19, 2021) says it depends on whether the copying puts the maker's investment at risk. A site that sends traffic back has an argument, but it's a court's call | Reported, earlier note |
| Is linking to an ad itself a copyright problem? | No, for a freely accessible page (*Svensson*, C-466/12); the agencies' contract terms are separate | Reported, earlier note |
| Photos and descriptions? | Copyright (photos 25 years even if "mere photographs", LPI art. 128). Never copy | Reported, earlier note |
| Personal data in ads (agent names, phones, private sellers)? | Collecting it by scraping is processing under GDPR (AEPD, 2025). Don't store it | Reported, earlier note |

## Recommended design
**What we store per home (facts only):** source agency, its own reference, the advert's address (URL), price, built m², bedrooms, bathrooms, type (flat, house, etc.), municipality and postcode or district, features (lift, terrace, parking, garden, pool, storage room, accessible), energy rating, first seen, last seen, status (on sale, gone). No photos, no description text, no agent names or phone numbers, and no street address unless the agency gives one.

**How it fits a group:** compare each home with the group's brief (`Matching.Brief`, which already gives bedrooms, bathrooms, m² range, areas, home types and must-have features). Hard filters are bedrooms, bathrooms, type and area. The score adds m² fit, must-haves met and nice-to-haves met. Price waits for the price band (B-043), and filtering homes by a group's budget filters homes, not people. Each home shows "why it fits" and a "See the advert at <agency>" link that opens the agency's own page. The referral is the agency's contact form, used by the group itself.

**How homes get in, in order:**
1. **Made-up homes (now, Build mode):** a generator like the made-up people (D-029), so the whole flow can be built and tested.
2. **Hand-seeded real homes (if F-017 allows):** someone types the facts and link of a few dozen homes in the pilot area from agencies' own sites, not portals. Taking a few dozen homes from many agencies is unlikely to be a "substantial part" of any one agency's database (Assumption, to confirm with the lawyer). A weekly manual check marks gone homes.
3. **Agency feeds (needs Sam's outreach):** one importer for Kyero V3 XML, the format Spanish agency CRMs already export, so each agency that says yes is a new feed URL, not new code. Run daily. The partner pitch: free, qualified buyer groups; no fee in Phase 1 (D-010).
4. **Permitted crawling:** only for an agency that gives written permission but has no feed. Identify ourselves, honour robots.txt, about one request every few seconds, and stop at once on request.
5. **Licensed data:** Casafari (from €49 a month, earlier note) if we need coverage quickly; needs Sam's spend and confirmed display rights.

Countries beyond Spain: the importer and fields don't depend on Spain, but D-001 keeps us in Spain.

## Open questions
- Lawyer (add to B-003): may we hold a small, hand-made index of facts with links? Do agencies' "no deep links" clauses bind us? Would a referral arrangement make us a property intermediary (R-003)?
- Which CRMs do Madrid agencies use, and can a new "portal" receive their Kyero feed for free? Inmovilla and Witei not yet checked (Partnerships, B-021 style).
- Engel & Völkers' terms (page loads by script; not read).

## Sources (checked 2026-11-22)
- robots.txt of idealista.com, fotocasa.es, habitaclia.com, pisos.com, tecnocasa.es, century21.es, remax.es, engelvoelkers.com, redpiso.es, donpiso.com, solvia.es, alisedainmobiliaria.com, servihabitat.com, comprarcasa.com, kyero.com, casafari.com, subastas.boe.es (BOE auctions: blocks all crawlers).
- Legal notices: tecnocasa.es/legal/avisolegal.html; redpiso.es/lopd/aviso-legal; donpiso.com/aviso-legal; remax.es/condiciones-de-uso; solvia.es/es/aviso-legal (6.2); alisedainmobiliaria.com/sobre-aliseda/aviso-legal; servihabitat.com/es/terminos-generales-de-uso; century21.es/terminos-y-condiciones; engelvoelkers.com/es/es/aviso-legal (not readable).
- Ley de Propiedad Intelectual, art. 133: boe.es/buscar/act.php?id=BOE-A-1996-8930.
- Earlier evidence: `research/2026-11-18-homes-sourcing.md`, `research/2026-11-20-independent-review.md`.
