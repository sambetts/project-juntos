# Compicasa PoC: the working app

The working proof of concept (B-028, B-029), hosted in Azure as if it were production (D-027), on Azure's free tiers (D-030). A C# backend with one installable web app for phones and desktops (D-028), filled with made-up people that are refreshed every morning (D-029). Spanish first, English supported (D-024). By the Builder, 2026-11-22.

**Live:** https://app-compicasa-prod-25yr5.azurewebsites.net. It's closed: creating an account needs the invite code (see "Operating it"). Hidden from search engines. Made-up data only: no real users or real personal data without Sam (D-027).

## What it does
- **Accounts:** sign up with a username, password, name to show and invite code; sign in and out; delete the account (removes likes and the groups you started). Passwords are hashed (ASP.NET Core Identity's hasher); the session is an HTTP-only, SameSite=Strict cookie.
- **Profile:** the prototype's v0.3 profile, saved to the database as you change it, plus a name and an optional bio. Two examples to start from.
- **Matches:** dealbreakers first; everyone else ranked by the demo score, with "why you click" and "what to talk about". Other people's dealbreaker settings, sliders and pickiness are never sent to the browser.
- **Consent:** your like is always explicit. Made-up people reply at once by simulation, as in the prototype (they accept if no dealbreaker applies and the score meets their pickiness). Real people reply with their own likes, so a match needs both to say yes.
- **Groups:** suggested groups from your mutual matches; preview the home brief, or propose the group. Made-up members answer at once; real members accept or decline an invitation. A group is "formed" once everyone says yes.
- **Home brief:** bedrooms, bathrooms, parking, useful area (editable by the group), must-haves, room allocation, placement tips and house-agreement starters.
- **Made-up people (D-029):** the 14 hand-written profiles from the prototype, always; plus a batch of 30 generated profiles, replaced each day after 04:00 Madrid time. Anything involving a replaced profile (likes, groups) goes with it. Generated names are common Spanish first names combined at random; bios are assembled from fragments in both languages.
- **Phones:** a PWA: it can be added to the home screen on Android and iOS, opens full screen, and its shell works offline (people and groups always come fresh from the server).

## How it's built
| Part | What | Where |
|---|---|---|
| Matching engine | The prototype's logic block ported to C#, pure functions. Returns message codes, not sentences, so all wording stays in the front end | `src/Compicasa.Core/Matching.cs` |
| Made-up data | The 14 seed profiles (exported from the prototype) and the daily generator | `src/Compicasa.Core/seed-profiles.json`, `DemoGenerator.cs` |
| API | ASP.NET Core minimal API on .NET 10 (LTS); EF Core on Azure SQL (SQLite locally and in tests) | `src/Compicasa.Api/` |
| Front end | Plain HTML, CSS and JavaScript (no build step), served by the API from the same origin | `src/Compicasa.Api/wwwroot/` |
| Infrastructure | Bicep: one resource group in Spain Central | `infra/main.bicep`, `infra/resources.bicep` |
| Deploy | Tests, publish, zip deploy, health wait; idempotent | `infra/deploy.ps1` |

**Azure (rg-compicasa-prod), free tiers (D-030):** App Service plan F1 (free) Linux in **West Europe** with one web app (HTTPS only, TLS 1.2, FTP off, system-assigned managed identity); Azure SQL Database on the free offer (serverless, 100,000 vCore-seconds and 32 GB a month, pauses instead of billing if that runs out) in **France Central**, the next region over (D-031: on 2026-11-22 no EU region offered both free tiers on this subscription; F-015 asks how to get to one region); Entra ID sign-in only, so no database password exists anywhere; Application Insights on a Log Analytics workspace capped at 0.1 GB a day; a €5 monthly budget alert to the subscription's owners.

**Cost:** expected €0. F1 and the SQL free offer don't bill. Logs are capped at about 3 GB a month, inside Log Analytics' free 5 GB per billing account (Reported; the allowance is shared with any other workspace on the same billing account). On 2026-11-22 it ran for a few hours on B1 and SQL Basic (about €16 a month at list prices, VAT not included), so expect a few cents on this month's bill.

**Free-tier limits:** the app sleeps after about 20 idle minutes, and the database pauses after 60. The first visit after a pause can take up to a minute while both wake. F1 allows 60 CPU minutes a day, after which the app stops until the next day. The daily refresh of made-up people runs when the app wakes, not at exactly 04:00. The data is in the EU (France), not in Spain.

**Security:** strict Content Security Policy (no inline scripts or styles; colours and sizes are set from script), `X-Frame-Options: DENY`, `nosniff`, no referrer, HSTS; every changing API call needs the `X-Compicasa: 1` header (blocks cross-site requests); sign-in and sign-up rate-limited to 10 a minute per address; server-side validation of every profile field; `noindex` on every response. The SQL server accepts connections from Azure services, and sign-in needs an Entra ID identity with a database user (only the web app and the admin have one).

## Run it locally
Needs the .NET 10 SDK. From `app/`:
```
dotnet test
cd src/Compicasa.Api
$env:Auth__InviteCode="local-test"; dotnet run     # PowerShell; uses SQLite (compicasa.db)
```
Then open the URL it prints and sign up with invite code `local-test`.

## Tests
- `dotnet test` (19 tests): **parity** with the prototype (all 240 pairings of the 14 profiles and the two examples score, match and explain exactly as the prototype; group suggestions, briefs and group checks too); fairness (household type and accessibility needs never change a score; scores are symmetric); the **API** (sign-up needs the invite code; hashed passwords; requests without the header are refused; private settings never leak; simulated replies; real people answer for themselves; outsiders can't see a group; deleting an account; security headers); the **daily refresh** (a batch is valid and repeatable; it's replaced after 04:00 Madrid time, and its likes and groups go with it).
- `node tests/e2e/e2e.mjs <url> <invite-code> [screenshot-dir]` (24 checks): drives headless Edge or Chrome at phone size (390 × 844) and desktop size: sign-up (wrong code refused, in Spanish), profile example, matches, liking, a suggested group's preview, proposing it, then the same account on desktop; Spanish by default with no English leaking, and vice versa; no sideways scrolling; colours applied under the strict CSP; manifest, icons and service worker; no script errors. Deletes its account at the end.
- To refresh the parity fixture after changing the prototype's rules: `node tests/parity/export-from-prototype.js` from the repository root.

## Operating it
- **Deploy:** `infra/deploy.ps1` (both), `-Infra` or `-App`. The app step runs the tests first and won't deploy if they fail.
- **Invite code:** kept only in the web app's settings, never in this repository. Read it with
  `az webapp config appsettings list -g rg-compicasa-prod -n app-compicasa-prod-25yr5 --subscription 81209232-7da9-4f81-b984-7ab0bd3dcfb3 --query "[?name=='Auth__InviteCode'].value | [0]" -o tsv`.
  Replace it with `deploy.ps1 -Infra -NewInviteCode`. Giving it to anyone but Sam means real users, which needs Sam's decision first (D-027).
- **Health:** `/healthz` (the app is up) and `/healthz/ready` (it can reach the database; slow if the database is paused).
- **If it outgrows the free tiers:** in `infra/resources.bicep`, set the plan to B1 with `alwaysOn: true` (about €12 a month) and, if the database pauses too often, drop `useFreeLimit`.
- **Logs:** Application Insights `appi-compicasa-prod`.

## Not done yet (in the backlog)
- Deploying from GitHub Actions instead of a laptop (B-030).
- Phones and screen readers checked by a person, and a native speaker's review of the new Spanish (B-025).
- Native store apps, if ever needed: wrap this PWA (B-032).
- Before any real user: the DPIA (B-027), a privacy notice, and Sam's decision.
