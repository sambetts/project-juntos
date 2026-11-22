# Homes index: how it works

_(Product with the Builder, Session 15, 2026-11-22.)_ The high-level picture. Operating details are in `app/README.md`; the evidence and law behind it are in `research/2026-11-22-listings-index.md`; the decision is D-035.

## What it's for
Groups agree what their shared home needs (the home brief). The homes index shows them real homes for sale that fit that brief, and sends them to the agency selling each one. We don't sell homes, take leads or show adverts: every home links back to the agency's own advert, and the group contacts the agency there.

## The rules (D-035)
- **Agencies' own websites only, never portals.** Idealista, Fotocasa, habitaclia, pisos.com and similar are refused in the code.
- **Facts and the link only.** We keep price, size, bedrooms, bathrooms, type, town, postcode, district, a few features (lift, parking, terrace, garden) and the energy rating. We also keep the agency's name and the advert's address. Never photos, description text or agents' contact details.
- **Always credit the agency.** Every home shows its agency and a "See the advert at <agency>" button.
- **Polite crawling.** It identifies itself as CompicasaBot, honours robots.txt, waits 3 seconds between requests, and only re-reads adverts that changed. Its `/bot` page explains what it does and how to block it.
- **Stop at once if asked.** Adding an agency to the `Homes__Stopped` setting hides its homes and stops the crawler visiting it; then its rows are deleted (R-023).

## How a home gets from an agency to a group
```
Agency website                      Our crawler (run by hand for now)          Our database
--------------                      ---------------------------------          ------------
robots.txt  ---------------------->  check what we may read
sitemap (adverts + last changed) ->  list homes for sale in Madrid province
advert page (changed ones only) -->  read the facts  ----------------------->  Homes table
                                     adverts gone from the sitemap ---------->  marked gone

Group page                          The app
----------                          -------
"Homes for sale that fit"  <-------  brief (beds, baths, m², areas, types, must-haves)
                                     x homes not gone, not stopped
                                     -> hard rules, then a score
"See the advert at <agency>" ------>  opens the agency's own page in a new tab
```

1. **Find:** each agency network has a small adapter. It reads the agency's sitemap, which lists every advert and when it last changed, and keeps homes for sale in the province of Madrid.
2. **Read:** it fetches only new or changed adverts and pulls out the facts. Tecnocasa's pages carry them as JSON; other agencies will need their own adapter (B-048).
3. **Store:** one row per home, with when we first and last saw it. Homes that drop out of the sitemap are marked gone and stop showing.
4. **Place:** the town and postcode put each home in one of the app's five Madrid areas (central Madrid inside the M-30, the rest of the city, north and west towns, south towns, the Henares corridor). This is approximate until real geography arrives (B-043).
5. **Match:** for a group, a home must have enough bedrooms, be a type and in an area the group accepts. It then scores out of 100 for bathrooms, size against the group's m² range, and must-haves the advert confirms; nice-to-haves add a little. Things an advert can't tell us (near the Metro, a quiet street) are listed for the group to check. Price waits for the group price band (B-043).
6. **Show:** real homes come first, best fit first, and the 90 made-up homes ("Agencia de ejemplo", no link) fill any gaps, so the demo always has something.

## Where it lives
| Part | File |
|---|---|
| Fit rules and Madrid areas | `app/src/Compicasa.Core/Homes.cs` |
| Crawler, robots.txt, portal block list, Tecnocasa adapter, made-up homes | `app/src/Compicasa.Api/Homes.cs` |
| Homes table | `app/src/Compicasa.Api/Data.cs` (migration `Homes`) |
| Group endpoint | `GET /api/groups/{id}/homes` in `app/src/Compicasa.Api/Api.cs` |
| Group page section | `homesSection` in `app/src/Compicasa.Api/wwwroot/app.js` |
| Crawler's public page | `app/src/Compicasa.Api/wwwroot/bot.html`, served at `/bot` |

## Running it
For now someone runs the crawler from a machine, against the local or live database (commands in `app/README.md`, "Crawling homes"). For the live database, that machine needs a temporary SQL firewall rule and the Azure CLI signed in as the database admin. Next: a daily run from Azure or GitHub Actions (B-047), then more agencies (B-048), then proper feeds (Kyero XML) from agencies that agree.

## Not yet
- Price and budget matching (B-043, after the lawyer on money filters: D-009).
- Agencies other than Tecnocasa (B-048).
- The lawyer's view on the database right, deep links and intermediary status (lawyer brief, A7).
