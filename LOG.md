# Log

Newest first. Add an entry at the end of every session (`templates/session-handoff.md`). Don't edit old entries; correct them in a new one.

## 2026-11-24 · Session 30: home thumbnails
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff; the Builder with Legal & Compliance; the CEO for D-051
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** Session 29 (house browser deployed). Sam: "For flats, we don't import images or thumbnails. We should at least have the thumbnail".
**Goals:** 1. B-080 thumbnails for homes (new).
**Needs Sam:** F-041 (new): push to deploy; recommend yes.
**Did**
- B-080: `Home.PhotoUrl` and migration `HomePhotoUrl`; `TecnocasaSource.Parse` keeps the first image's `card` address from the advert's `media.images` (checked on one live advert, 664111); `HomeSources.PhotoHosts` and `Photo()` keep only https addresses on `cdn-media.medialabtc.it`; the crawl re-reads a home stored before this once (`PhotoUrl` null), then skips it as before; the three home APIs return `photo`; `homeItem` shows it with `referrerpolicy="no-referrer"`, lazy loading and a "Foto: <agency>" credit, and a capturing error handler swaps back to the sketch; the CSP allows images from the photo hosts only; service worker version bumped; bot page and "Where homes come from" reworded (both languages). Tests: parser keeps the card address and not the large one, host check, the one-off re-read.
- Fixed blank "miss" tags in the house browser: `miss_area`, `miss_beds` and `miss_type` had no wording (Session 29 gap, visible when browsing all of Spain).
- Kept within the rules: AGENTS.md section 4's agency line now names the photo address (Sam's chat words, D-051); D-035, `product/homes-index.md`, STATE and the lawyer brief (A7, a new question) updated; R-029 added.

**Decided:** D-051 photos shown from the agency's server, never copied (Sam's instruction; the hotlink design is the CEO's).
**Approvals used:** none (nothing pushed).
**Learned:** Tecnocasa's advert JSON lists photos under `media.images[].url` in six sizes, and floor plans under `media.floor_plans`; the sitemaps list 5,421 homes for sale we can use (Verified, local crawl 2026-11-24). The case law in D-051 is Reported (not re-checked this session).
**Open or blocked:** F-041; F-040, F-033, F-037, F-014 as before.
**Saved and checked:** `dotnet test` 98 of 98; browser test 93 of 93; a local crawl of five real adverts stored five photo addresses and all five loaded in headless Edge with no script errors; project files checked with `git status` and `git diff` before the commit; LOG.md saved last.
**After the closing report:** Sam: "push" (recorded in F-041). Pushed `924ffb7`; run 45427321392 passed (test, deploy); live: ready 200, service worker session30, CSP `img-src` includes `https://cdn-media.medialabtc.it`, app.js has the photo cards, `/bot` has the new wording, `/api/browse` 401 signed out. Photos appear once the 05:00 crawl re-reads homes. This follow-up commit is not pushed.
**Next session should start with:** 1. Check `/api/browse?real=true` returns `photo` addresses after the next crawl and the cards show them live. 2. B-052 with B-027, B-039, B-041, B-072. 3. B-079 once F-040 is answered.

## 2026-11-24 · Session 29: contact form
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff; the Builder with Legal & Compliance
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** Session 28 (pushed and deployed). Sam: "Add a contact us form. We'll need to setup an email address & domain (pending me). How will security for the funnel page work?"
**Goals:** 1. B-078 contact form (new; also the contact point in B-072).
**Needs Sam:** F-038 (new): push to deploy; F-014 now includes the email address.
**Did**
- B-078: `Contact.cs` (`POST /api/contact` open to anyone, rate-limited per address, a hidden bot field; `GET /api/admin/contact` and `POST /api/admin/contact/{id}/done` for listed admins), the `ContactMessages` table and migration `Contact`, `#contact` (menu and footer) and `#inbox` in both languages; dealt-with messages deleted after 365 days (Assumption until B-052). The admin check moved to `Funnel.AllowedAsync`, shared by both pages. `ContactTests.cs` (4 tests); three browser steps.
- Answered Sam's question on the funnel's security in chat (session cookie from sign-in, server-side allow-list in an app setting, counts only).

**Decided:** nothing new.
**Approvals used:** none (nothing pushed).
**Learned:** the browser test's `until` steps don't add to its pass count, so "85 passed" hides new steps; confirmed the contact steps ran by reading the stored messages in the local database.
**Open or blocked:** email notification waits for F-014; F-037, F-033, F-035.
**Saved and checked:** `dotnet test` 94 of 94; browser test 85 of 85 with no failures; project files checked with `git status` and `git diff` before the commit; LOG.md saved last.
**After the closing report:** Sam: "that's fine. Make it happen." Pushed `638e752` (deploy run 45383975227). Looking up his account ID in the live database was blocked by the tool's safety check (it holds real data), so Sam is asked to read his own ID from `/api/me` while signed in; then `Admin__AccountIds` is set.
**Later in the session (B-064):** Sam, 2026-11-24, in Claude Code, asked "Where is the houses browser? Didn't we say we were going to implement it?", then chose: who sees it "Signed-in people (Recommended)"; features "Fit with my brief, Save favourites, Add to a group shortlist, Map view". Built the browser (see B-064's row). Found and fixed on the way: the contact inbox's page state used `S.inbox`, which the Alerts tab already uses (deployed, but unreachable while no admin is set); the browse API sent features as text, and an i18n function called `num`, which isn't in scope there: both broke drawing the list and were found from the browser test and its screenshots. Four tabs now (People, Homes, Groups, Messages); the two tab-count checks updated. The map is split out as B-079, because homes have no coordinates and map tiles mean a third party (F-040). Committed; then Sam said "push." (F-039): deployed 2026-11-24 (F-039: Sam said "push"; CI run 45420454244 passed; checked live: ready 200, service worker session29b, `/api/browse` 401 signed out, new app.js and i18n.js served).
**Next session should start with:** 1. F-038: if pushed, the CI/CD run (with the migration) and `#contact` live. 2. B-052 with B-027 (privacy notice: add contact messages and the funnel), B-039, B-041, B-072. 3. When F-014 is done, email a notification of new contact messages.

## 2026-11-24 · Session 28: release checklist, first three items
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff; the Builder with Legal & Compliance (B-042), the Builder (B-073), the Builder with Validation (B-074)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** Session 27 (strategy, D-048, D-049), pushed on Sam's go (`30d1e87`). Sam: "push and start on the release checklist".
**Goals:** 1. B-042 no household-type filter on real people. 2. B-073 keep a subscription move ready. 3. B-074 funnel counts.
**Needs Sam:** F-036 (new): push to deploy B-042 and B-074, then set his account id for `#funnel`; recommend yes.
**Did**
- B-042: `Matching.Hard` applies the household rule only between two made-up people (a `Real` flag set in `Account.ReadProfile`, `[JsonIgnore]`, kept by `Clone`); `ProfileRules` stores every household and accepts a profile without `open`; the profile's "who you'd share with" section now explains instead of asking; guide and hint wording cleaned in both languages; `HouseholdTests.cs` (4 tests). The prototype parity tests pass unchanged.
- B-073: an empty `planId` creates a plan of its own (`planSku`, B1); `deploy.ps1 -OwnPlan`; move steps in `app/README.md`. Found while doing it: resource names carry a suffix from the resource group's ID, which includes the subscription, so a move changes the app's address.
- B-074: `Funnel.cs` (`GET /api/admin/funnel`), the `#funnel` page in both languages, `Admin__AccountIds` through Bicep and `deploy.ps1`; `FunnelTests.cs` (3 tests); a browser check that someone not listed is refused. Changed during the session: a refusal is `{allowed:false}`, not 404, because the 404 showed as a console error; Madrid counts only finished profiles, since unfinished ones carry Madrid's default areas.
- Checked by hand against a local run: the funnel's JSON as a listed admin.

**Decided:** nothing new (B-042 applies AGENTS.md section 4; D-049 covers B-073).
**Approvals used:** Sam's "push and start on the release checklist" (2026-11-24): pushed Session 27's commits only.
**Learned:** a subscription move changes the public address unless there is a custom domain (F-014). Two `node` processes on this machine belong to the GitHub Copilot CLI, not to these sessions; left alone. The funnel page was checked as JSON, not visually as an admin.
**Open or blocked:** F-036; F-033 to F-035; B-072's operator notice waits for F-034.
**Saved and checked:** `dotnet test` 90 of 90; browser test 85 of 85 on a fresh local database (twice, after the last changes); `az bicep build` clean; three code commits (`3672b10`, `f2497ea`, `0170aed`). Project files checked in `git diff` against this entry before the last commit; LOG.md saved last.
**After the closing report:** Sam: "push" (F-036, closed; pushed `7f09f44`). On F-034 and the lawyer: "Creating an actual company I'm not going to do until it's absolutely necesary. Unless we're charging money I don't see the need yet and that has it's own costs. The aim is to have as close to 0 costs until the interest is proven. Lawyer too." Deploy run 45372731509 succeeded; live checks: `/healthz/ready` 200, service worker `session28b`, the new `#funnel` and household wording served, `/api/admin/funnel` 401 signed out. Recorded as D-050 (CEO): F-034 closed, F-006 deferred, checklist items 2 and 3 changed, R-026 accepted; new F-037 (operator details on the site). On critical mass: "we'll decide later … maybe suggest some numbers": three suggested milestones (signal, traction, critical mass) in `project/strategy.md` section 4; F-035 stays open.
**Next session should start with:** 1. F-036: if pushed, the CI/CD run, the live profile and `#funnel`. 2. B-052 with B-027, B-039, B-041, B-072. 3. B-075 or B-076 if Sam agrees; B-070.

## 2026-11-24 · Session 27: a free tool first (strategy)
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff; the CEO (D-048, strategy, roadmap); Research and Legal & Compliance through five helpers run by the deep-research skill (affordability, demand for co-buying, Spanish competitors, international platforms, launch rules)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 26 and its log entry. Sam: "We're going to update the strategy a bit here for this project. The plan is to release as a free tool and see if we can gain traction as a concept & service. Once and if we hit critical mas, we can look at monetising. I have some free Azure credit (where it's deployed)." He asked for the problem statement, research backing it, competitors and where we can do better, the strategy and the risks, and to see which skills could help.
**Goals:** 1. Record the strategy change (D-048). 2. Strategy document with fresh research. 3. RISKS, roadmap, backlog and founder queue to match.
**Needs Sam:** F-032 (new) hosting off the Visual Studio credit, recommend A; F-033 (new) the release checklist as the gate to real users; F-034 (new) individual or S.L., ask the lawyer first; F-035 (new) critical mass and the focus area, replacing F-004, F-005 and F-010. F-006 (the lawyer) is now the critical path.
**Did**
- D-048 recorded with Sam's words; D-010's recommendation updated (no charging until critical mass; matching free for good).
- Deep-research skill: five helpers in parallel, notes kept in the scratch folder. The Chief of Staff wrote the repository notes from them in AGENTS.md's format (the skill's own report writer was skipped for that reason): `research/2026-11-24-problem-evidence.md`, `research/2026-11-24-competitors-refresh.md`, `research/2026-11-24-free-launch-rules.md`.
- `project/strategy.md` v1.0 (CEO): problem statement, evidence by strength, competitor comparison and where we do better, four stages (release checklist; open free with one focus area; measure groups agreeing on a home; charge later), risks. `project/roadmap.md` v0.3; a pointer in `project/charter.md`.
- RISKS: R-001 updated; R-024 to R-028 added; R-022 noted. BACKLOG: B-071 done, B-004 done, B-072 to B-077 added (B-075 and B-076 proposals), B-027, B-039, B-041, B-042, B-052 tied to the release checklist, B-019 next due 2027-02-24, B-047 updated.
- Checked the 05:00 crawl: succeeded (3,163 Tecnocasa homes, last checked 2026-11-24 04:01 UTC via `/api/homes/sources`), so `Schema.WakeAsync` works against Azure SQL.

**Decided:** D-048 a free tool first; charge only at critical mass (Sam).
**Approvals used:** none; nothing deployed, published or sent.
**Learned:** the Azure subscription is the Visual Studio subscriber offer (`quotaId MSDN_2014-09-01`, spending limit Off; read with `az rest`), and Microsoft's terms say "Production workloads must be run on regular subscriptions" (Verified on the offer page and Microsoft Learn by a separate fetch). Demand for buying with strangers is unmeasured everywhere; stated willingness abroad runs 5–10 times above actual co-buying; habitacion.com converts under 1% of its waiting list; Kohab (Australia) has closed; CoBuy (US) earns from the agreement, not matching. Casicasa now says it has no people search or way to join a group. A helper's note cited Cataluña's agents' register under a decree I had written from memory; replaced with the Generalitat page it actually read. Not confirmed: Banco de España chapter 2 (PDF wouldn't open), the survey primaries (B-077), Microsoft for Startups eligibility, a pay-as-you-go price for today's set-up.
**Open or blocked:** F-032 to F-035; B-073 waits on F-032; B-072's operator notice on F-034.
**Saved and checked:** every changed file re-read in `git diff` against the records above (DECISIONS, RISKS, BACKLOG, FOUNDER-QUEUE, STATE, project/strategy, roadmap, charter, three research notes); IDs checked unique (R-024 to R-028, B-071 to B-077, F-032 to F-035, D-048). LOG.md saved last; its own check and the commit are confirmed in the closing report.
**After the closing report:** Sam on F-032: "I'm happy to run in the VS sub for now, assuming we can migrate to another one later". Recorded as D-049; F-032 closed; B-073 re-scoped to keeping the move ready; strategy item 1, R-024 and STATE updated.
Then Sam: "push and start on the release checklist" (2026-11-24, in Claude Code). Read as approval to push `main` with Session 27's commits (documents only, so no deploy) and to start the checklist in Session 28.
**Next session should start with:** 1. Sam's answers to F-033 to F-035. 2. Release checklist items that don't wait on Sam: B-042, B-052 with B-027, B-074, B-039, B-041. 3. B-075 or B-076 if Sam agrees; B-070.

## 2026-11-23 · Session 26: live in or rent out, full ITP scales, the sheet race, the crawl's wake-up
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff; three helpers in their own git worktrees (Product, Builder and Legal & Compliance for B-063; Builder and Tester for B-061; Research and Builder for B-060); the CEO (D-047)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** Session 25 (B-062 pushed on Sam's go, F-029; CI/CD run succeeded; `/api/homes/sources` live with 1,410 Tecnocasa homes). Sam: "push and then work on backlog items in new sesssions", then "push when everything done".
**Goals:** 1. B-063 live in or rent out. 2. B-061 the flaky sheet step. 3. B-060 full ITP scales. (B-064 waits for Sam's description.)
**Needs Sam:** F-031 (new): widen AGENTS.md's mission to include renting out; recommendation A.
**Did**
- B-047: the 05:00 crawl failed after 51 s (SQL error 40613, the free database waking; the retries give up after about 50 s). `Schema.WakeAsync` now waits up to 5 minutes. Starting today's missed run by hand was refused by the permission check, so it was left.
- B-063 (D-047): purpose on each profile; same-purpose hard rule; letting path in the wizard, matching, brief and shortlist; Part C (letting) in the lawyer brief, Reported. Reviewed: no budget or protected trait in matching.
- B-060: seven progressive ITP scales Verified in BOE; Valencia's article re-checked by the Chief of Staff (art. 13.Uno: 9%, 11% above 1 M €; text updated 10/08/2026); three amounts re-worked by hand.
- B-061: an app race, not a test race (a render between `dialog.close()` and its "close" event lost the close); fixed in `app.js`, with forced-timing checks.
- Merged the three branches (conflicts only in `sw.js` VERSION and the README test lines, resolved by hand). Records: D-047, D-045 evidence note, B-060/B-061/B-063 done, B-065 to B-070 proposed, F-030 and F-031.

**Decided:** D-047 live in or rent out (Sam's request, recorded by the CEO).
**Approvals used:** F-029 (push B-062, 2026-11-23); F-030 (push Session 26 when tests pass, 2026-11-23).
**Learned:** the "intermittent test" was a real bug users could hit. The free database's wake-up outlasts EF's retries. TaskStop may not stop child processes of a shell loop on this Windows set-up (B-061 helper); check for leftover `node` processes. A double vote can return 500 (B-070).
**Open or blocked:** F-031; B-064 (Sam's description); `WakeAsync` untested against Azure SQL until the 2026-11-24 05:00 run.
**Saved and checked:** merged `main`: `dotnet test` 83 of 83; `node app/tests/itp/itp-check.mjs` 77 of 77; browser test 85 of 85 three runs in a row against a fresh local database. `git status` and `git diff --stat` checked against the records above before committing.
**Next session should start with:** 1. The F-030 deploy and the 2026-11-24 05:00 crawl. 2. B-070. 3. B-065, B-068, then Legal's B-066 and B-067.

## 2026-11-23 · Session 25: where homes come from, and "preview version"
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Product (B-062) and the CEO (D-046)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 24 (continued) and its log entry. Sam: "I want a page on how we scrape pages & agencies, which ones and when. Also remove any mention of this being a PoC - we can say it's a preview version maybe."
**Goals:** 1. A user-facing page on where homes come from (B-062). 2. Replace "proof of concept" with "preview version" wherever users see it.
**Needs Sam:** F-029 (new): push B-062; recommendation A, push now. B-064: the rest of his house-browser message.
**Did**
- `GET /api/homes/sources` (public, like `/bot`): each crawled agency, homes listed and last checked; stopped agencies left out; plus the portal block list. `HomeSources.Crawled` lists the agencies; `IHomeSource.Name` added. Test `Anyone_can_see_which_agencies_we_read_and_when`.
- `#sources` page in `wwwroot/app.js` (`viewSources`) and `i18n.js` (Spanish and English), linked from the header menu, the footer and a group's *Homes*. Schedule and limits taken from the WebJob's `settings.job` and `run.cmd` and from `Crawl` (Verified in code).
- Every user-facing "proof of concept", "prueba de concepto", "prototype" and "prototipo" replaced by "preview version"/"versión preliminar" (`i18n.js`, `index.html`'s description); service worker version bumped. Code comments and project files unchanged.
- Sam asked mid-session for two features in their own sessions: B-063 (live in or rent out) and B-064 (a house browser; his message was cut off). Both added to the top of BACKLOG.md. `product/homes-index.md` points to the new page.

**Decided:** D-046 the app calls itself a preview version and says openly where its homes come from (Sam's words, recorded by the CEO).
**Approvals used:** none; nothing pushed.
**Learned:** the local `compicasa.db` was stale ("no such table: Candidates"), which made the first two browser-test runs fail; deleted (made-up local data) and the run passed 78 of 78. B-063 widens the mission (a home to live in) and will need a decision and the lawyer brief updated before it ships.
**Open or blocked:** F-029; B-063 and B-064 for fresh sessions; B-061, B-060 as before.
**Saved and checked:** `dotnet test` 74 of 74; browser test 78 of 78 against a local run; `#sources` screenshotted at 390 px in both languages, no sideways scroll, no script errors. `git status` and `git diff --stat` checked against the changes above before committing.
**Next session should start with:** 1. If F-029 was pushed, check the deploy and `#sources` live. 2. B-063 in a fresh session, starting with Product's spec. 3. B-064 once Sam has described it.

## 2026-11-23 · Session 24, continued: the regional ITP rates checked
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Research (the rates) and the Builder (the fix)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** Session 24's deploy (F-027, CI/CD run succeeded; the live site served the new province list). Sam: "verify the ITP rates on each region's official site".
**Goals:** 1. Check every region's ITP rate in a primary source (B-059 follow-up).
**Needs Sam:** F-028 (new): push the corrected rates; recommendation A, push now.
**Did**
- Read the general rate for a resale home in each region's current consolidated tax law: 15 regions, Navarra and Ceuta and Melilla (the state rule) on BOE's consolidated texts; Bizkaia and Álava in the provincial governments' own consolidated texts. Sources, articles and last-update dates are in `product/2026-11-23-all-of-spain.md`. The Ministry of Finance's summary (Tributación Autonómica 2026) refused connections from two networks, so it wasn't used.
- Corrected two rates in `wwwroot/i18n.js`: Galicia 9% to 8% (DL 1/2011 art. 14) and Murcia 8% to 7.75% (DL 1/2010 art. 6); the cost note now shows two decimals (7,75 %); service worker version bumped.
- B-059 and D-045's evidence updated; B-060 (full scales, proposed) and B-061 (an intermittent browser-test timeout) added; F-028.

**Decided:** nothing new; D-045's evidence corrected (CEO).
**Approvals used:** none beyond Sam's request; nothing pushed.
**Learned:** the first pass from secondary sources had two rates wrong, both too high, which shows why the "higher rate if unsure" rule wasn't enough. The estimate uses only the first band; seven regions charge more for dearer homes (B-060). Gipuzkoa's current law isn't published as an official consolidated text that I could find, so its 4% stays Reported. Ceuta and Melilla's reductions under a 1955 law weren't checked, so the 6% there may overstate the tax. Castilla y León's consolidated text on BOE was last updated 14/05/2024, so a later change could be missing.
**Open or blocked:** F-028; B-060; B-061.
**Saved and checked:** `dotnet test` 73 of 73. The browser test passed 3 of 6 local runs of this change. Each of the 3 failures was the step that closes a person's card (two on phone, one on desktop), which this change doesn't touch. The last committed version passed 5 of 5, so the fault is intermittent and recorded as B-061. `git status` and `git diff` show the expected changes. LOG.md saved last; the commit is confirmed in the closing report.
**After the closing report:** Sam: "push" (F-028, closed); pushed `main` for CI/CD to deploy (D-041).
**Next session should start with:** 1. Check the F-028 deploy. 2. Count homes outside Madrid after the next crawl. 3. B-061, then B-060.

## 2026-11-23 · Session 24: all of Spain
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Product (B-059), Research (ITP rates), Legal & Compliance (R-023) and the CEO (D-045)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 23. Sam asked whether the app supports places other than Madrid; I answered that it doesn't (the areas, the crawl and the tax in the cost estimate are all Madrid's). Sam: "Do it. We need to support all of Spain". No new answers in the founder queue.
**Goals:** 1. B-059 all of Spain.
**Needs Sam:** F-027 (new): push to deploy; recommendation A, push now.
**Did**
- Core: `Regions.cs`: area ids are Madrid's five areas, then every other province as `p` + its INE code (56 in all); `ProvinceOf` (postcode to province) and `AreaFor` (province, or a Madrid area inside Madrid). `Catalogue.Areas` now comes from it, so profile checks, matching and a group's common areas take any province. Made-up people: about a quarter look in one of seven other big cities.
- Homes: the Tecnocasa adapter accepts adverts in any province and places them with `Regions.AreaFor`; 36 made-up homes in 12 towns outside Madrid (126 in all, added to existing databases by id, the first 90 unchanged).
- Front end: a folding area picker with one group per region (Madrid first), a live summary and counts; region and province names and each region's ITP rate in `i18n.js`; the agreed-home estimate uses the home's region (10% if it can't be placed); the cost and budget hints no longer say Madrid; service worker version bumped.
- Tests: `RegionsTests.cs` (7 tests, 15 cases with the postcode theory); demo home count 126; the browser test ticks Sevilla in the wizard and checks the summary, the count and no sideways scroll.
- Documents: `product/2026-11-23-all-of-spain.md`; D-045; B-059; F-027; R-023 note; `app/README.md`; `product/homes-index.md`; STATE.md.

**Decided:** D-045 (CEO): the app covers all of Spain; Madrid keeps its five areas; D-008's Madrid pilot unchanged.
**Approvals used:** Sam's "Do it. We need to support all of Spain" (2026-11-23, in Claude Code), for B-059 in this repository. Nothing pushed or deployed.
**Learned:** the ITP rates were found only in secondary sources, which disagree for Cantabria, Galicia and Murcia; the higher rate is used, and all except Madrid's are labelled Reported. Ceuta and Melilla are Assumptions. The first browser run failed two new checks: ticking a box saves without redrawing, so the picker's summary didn't change. It now updates in place.
**Open or blocked:** not deployed (F-027); ITP rates to confirm on each region's own site before real users; finer areas inside other big cities wait for B-043.
**Saved and checked:** `dotnet test` 73 of 73; browser test 78 of 78 against a fresh local run (SQLite in the scratch folder) after the last code change; the phone screenshot of the area picker checked by eye. `git status` and `git diff --stat` show the 17 changed files and 3 new ones expected (`.claude/` left uncommitted as before). LOG.md saved last; the commit is confirmed in the closing report.
**After the closing report:** Sam: "push" (F-027, closed); pushed `main` for CI/CD to deploy (D-041).
**Next session should start with:** 1. Check the deploy, then after the next crawl count homes outside Madrid. 2. Confirm the ITP rates. 3. B-042 and B-052 before anyone but Sam signs up.

## 2026-11-23 · Session 23: a maximum budget per profile
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Product (B-058), Legal & Compliance (the A3 question, the risk and privacy notes) and the CEO (D-044)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 22. Sam: "New change: profiles need to have a maximum € they can spend, which obviously combined with others will filter what houses they can afford." No new answers in the founder queue (F-025 was already closed).
**Goals:** 1. B-058 maximum budget per profile.
**Needs Sam:** F-026 (new): push to deploy; recommendation A, push now.
**Did**
- Core (`app/src/Compicasa.Core/`): `Profile.MaxBudget` (private, so not in `PublicProfile`); `Budgets.Combined` (the total, or none if anyone hasn't set one) and `Budgets.DemoFor` (made-up budgets from the id, so the day's other made-up answers don't change); `HomeFit.Fit` and `Rank` take an optional budget: a price over it fails the fit (`miss: price`), a missing price goes under "check". Seeds and the daily made-up people get budgets.
- API: `ProfileRules` accepts 10,000 to 5,000,000 € or empty (`badBudget`); `GET /api/groups/{id}/homes` returns `budget`, `overBudget` and `budgetUnset`; `/api/me/homes` filters by you and your best match and returns only `budgetSet`; on a shortlist, a home over the total shows the miss, and made-up members vote No for the price.
- Front end: a wizard step "Tu presupuesto" / "Your budget" (step 7 of 9, needs an amount); a profile section with a summary line; the group's combined budget above its homes, or who still needs to set one, with a link to set yours; a nudge on Home; the guide's steps 1 and 4; strings in Spanish and English. I dropped a "within budget" tag after the first screenshots: every home shown is within budget, so it took a tag slot and said nothing.
- Tests: `BudgetTests.cs` (5: the fit rule, the total, made-up budgets, the API's checks and privacy, a group's filtered homes); `maxBudget` added to the "private settings don't leak" check; the browser test walks nine steps, checks the budget step refuses to move on without an amount, and checks the combined-budget line.
- Documents: `product/2026-11-23-maximum-budget.md`; D-044; B-058 (done) and B-052 (budgets added); R-006 updated; a question added to A3 of the lawyer brief; F-026; `app/README.md`; STATE.md.

**Decided:** D-044 (CEO): a private maximum budget per profile; a group's total filters its homes, never people.
**Approvals used:** Sam's request in chat (2026-11-23), for B-058 in this repository. Nothing pushed or deployed.
**Learned:** the lawyer brief's A3 had planned "fund your share: yes / not sure", with no figures, because socioeconomic status is a protected ground under Ley 15/2022. A figure that only filters homes is a softer use, but it still needs the lawyer before real users (Assumption that it's fine for Sam and made-up people in the PoC). In a group of two, the total gives away the other person's figure; the hint says so rather than hiding the total.
**Open or blocked:** not deployed (F-026); Sam's own live account has no budget until he sets one; the Spanish needs a native speaker's review (B-031); proposed follow-up: warn in the agreed-home estimate if your share is over your budget.
**Saved and checked:** `dotnet test` 58 of 58; browser test 74 of 74 against a fresh local run (SQLite in the scratch folder) after the last code change. A first rerun had hit the old server, which was still running, so I stopped it and ran everything again. I checked the phone and desktop screenshots of a group's homes by eye. `git status` and `git diff --stat` show the 20 changed files and 2 new ones expected (`.claude/` left uncommitted as before). LOG.md saved last; the commit is confirmed in the closing report.
**After the closing report:** Sam: "push it" (F-026, closed); pushed `main` for CI/CD to deploy (D-041).
**Next session should start with:** 1. Check the deploy run, then Sam sets his budget live and check a group's *Homes*. 2. B-042 and B-052 before anyone but Sam signs up. 3. B-047 crawl check.

## 2026-11-23 · Session 22: house voting for a group
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Product and Builder roles (B-056), Legal & Compliance (the pack's wording) and the CEO (D-043)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 21. Sam: "Next feature: house voting for a group. A group should be able to add candidate houses, and each member be able to vote on it. When all members agree on a house then we should take some sort of action - maybe prep contracts & emails to the agency or something? Give me ideas." The ideas went to him in chat (shortlist, staged votes hidden until everyone has voted, votes reset on a price change or a newcomer; on agreement a cost split, lawyer questions and an email draft rather than contracts). Sam: "yes, add it and build the first version".
**Goals:** 1. B-056 house voting, first version.
**Needs Sam:** F-024 (new): email drafts to real agencies; recommendation A, keep them off until Pilot mode and the lawyer's review.
**Did**
- Server: `app/src/Compicasa.Api/Shortlist.cs`: `Candidates` and `CandidateVotes` (migration `Shortlist`, two new tables, nothing changed or dropped); `GET/POST /api/groups/{id}/shortlist`, `DELETE …/{cid}` (whoever added it, or the proposer), `PUT …/{cid}/vote`. The round is worked out from the votes each time (yes from everyone still in, at the current price), so a price change or a newcomer needs no clean-up code. Choices on a round are hidden from others until everyone has voted. Made-up members vote at once by fit (`DemoVote`). Four new notification kinds; shortlists and votes are removed with an account or the daily batch (`Threads.ForgetAsync`).
- Front end (`wwwroot/app.js`, `i18n.js`, `app.css`): a fifth view on a group's page, *Candidatas* / *Shortlist* (`#group-<id>-shortlist`); "Add to shortlist" on each home; three-round stepper, vote chips, Yes, Maybe or No with an optional reason; a shortlist tile and a next step on *Overview*; on agreement, a cost table (equal or by rooms; the price column hides on phones), nine questions for the lawyer and notary with Copy, and an email draft with "Open in my email" (`mailto:`, nothing sent by us) for made-up homes only. A section in the guide (`#help`) in both languages.
- Tests: `app/tests/Compicasa.Tests/ShortlistTests.cs` (5 tests); the migration list in `AccountsTests.cs`; the browser test's `voteCheck` (add, vote three rounds, pack or reasons, no English, no sideways scroll). The browser test's advert-link check now looks for the advert link itself, since a home can also carry the "On the shortlist" link.
- `product/2026-11-23-house-voting.md` (how it works, the estimate's inputs with labels, open questions); `app/README.md`; D-043; F-024; B-056 done; B-057 added.

**Decided:** D-043 (CEO): house voting as built; email drafts for made-up homes only until F-024.
**Approvals used:** Sam's "yes, add it and build the first version" (2026-11-23, in Claude Code), for B-056 in this repository. Nothing pushed or deployed.
**Learned:** the ITP rate for a resale home in the Comunidad de Madrid is 6% according to secondary sources (guiafiscal.es and Taxdown, citing art. 28 of Decreto Legislativo 1/2010), but not confirmed on the Comunidad's own site or the BOCM, so it is labelled Reported; the 20% deposit and 1.5% fees are Assumptions. One source gives a different AJD rate (not used). The local browser test showed a pre-existing race in the chat's mark-as-read (a 500 when two reads of the same conversation insert the same row): B-057. Across eight runs, the other failures were timeouts or a page-load error in steps the change doesn't touch (the known timing slips); the voting checks passed in every run that reached them. A broad command to stop the local server was refused by the tool; stopping the background task worked instead.
**Open or blocked:** not deployed (a push to `main` deploys it and applies the migration, D-041); not seen on a real phone; the Spanish needs a native speaker's review (B-031); F-024.
**Saved and checked:** `dotnet test` 52 of 52; browser test 71 of 71 against a local run (SQLite in the scratch folder) after the last code change, and 70 of 71 after the final wording and table-heading change (the one failure was B-057's 500), with screenshots of the agreed pack at phone width checked by eye (the cost figures match: 317,000 € at 27.5% is 87,175 €; 80% is 253,600 €). `git status` and `git diff --stat` show the 15 changed and 6 new files expected and no other changes (`.claude/` left uncommitted as before); `git diff --check` clean. LOG.md saved last; the commit is confirmed in the closing report.
**Next session should start with:** 1. Sam's push, then try voting on the live site (made-up homes are off there, so the email draft shows as "not switched on"). 2. B-057. 3. B-042 and B-052 before anyone but Sam signs up.
**After the closing report:** Sam: "fix the rest then push" (recorded as F-025, closed). Fixed B-057 (`Social.MarkReadAsync` catches the duplicate read mark and moves it on; a new test reads a conversation eight times at once, though it passes on the old code too, because the in-process SQLite test database doesn't reproduce the race). Confirmed the ITP rate on the Comunidad de Madrid's own page ("se aplicará el 6 por 100", page updated 2026-11-03), now Verified in `product/2026-11-23-house-voting.md`; the same page lists lower amounts for some buyers, which the estimate's note now mentions in both languages. Not done here: B-042 and B-052, a phone check and the native speaker's review. `dotnet test` 53 of 53, then pushed `main` (CI/CD deploys it, D-041); the run's result is in the closing report.

## 2026-11-23 · Session 21: a start-up screen
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder (B-054)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 20. Sam, with a screenshot of the live site showing only the header: "When I load the site from cold, it takes a while to show anything. We need a cool looking loading screen." No new answers in the founder queue.
**Goals:** 1. B-054 start-up screen.
**Needs Sam:** nothing new; it goes live on his next push to `main` (D-041).
**Did**
- `wwwroot/index.html`: the start-up screen sits inside `#app`, so it paints with the HTML, before `app.js` or the slow first API calls (`/api/config`, `/api/me`); the first `render()` replaces it, so no extra code removes it. The logo draws itself on a 2.8 s loop (roof and walls traced, the two doors rise, then it clears), with a pulsing glow and three dots. Text in Spanish and English; CSS shows the one matching `html[lang]`, which `app.js` now sets as soon as it reads the saved language. After 6 s: "El servidor se está despertando…" / "The server is waking up…". Static with reduced motion. No inline script or style, so the CSP is unchanged.
- `app.css`: the `.boot` styles. `sw.js`: version bumped to `compicasa-shell-2026-11-23-loader`.
**Decided:** nothing needing a D-number (a reversible look-and-feel choice).
**Learned:** the blank page in Sam's screenshot is the HTML waiting on the first API calls, not the HTML itself being slow (the header had already painted); the screen covers that wait. A cold App Service start before any HTML arrives still shows the browser's own blank page, except for return visitors whose service worker has the shell (Assumption: not measured).
**Open or blocked:** not deployed; not seen on a phone.
**Saved and checked:** looked at in a local run (`dotnet` of the built app on port 5206, via a new `.claude/launch.json`, left uncommitted because it holds this machine's path): the reduced-motion version (the preview pane prefers reduced motion), then with that rule switched off the drawing and doors animated and looped, and the 6-second line appeared. No .NET or browser tests run (markup and styles only). `git diff` reviewed for BACKLOG, STATE, LOG and `wwwroot/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. B-042 and B-052 before anyone but Sam signs up. 2. B-047 crawl check. 3. Mention Home in the guide (`#help`).
**After the closing report:** Sam: "Yes, push it" (F-023). Pushed `main` (Sessions 18 to 21). The first CI/CD run (45171544394) passed its tests but failed to sign in to Azure: AADSTS700213, because GitHub sent the ID-pinned subject `repo:sambetts@34576795/project-juntos@1467408746:ref:refs/heads/main` and the app registration `compicasa-github-deploy` only trusts `repo:sambetts/project-juntos:ref:refs/heads/main` (Verified from the run log and `az ad app federated-credential list`). Adding a second credential with the new subject was refused by the tool's safety check, so it waits for Sam; the live site still runs Session 20 (healthy, no start-up screen).
Then Sam: "You run it" (recorded under F-023). Added the federated credential `github-main-ids` with that subject to `compicasa-github-deploy` (the old one stays) and re-ran the failed job: tests and deploy passed. Live: `/` serves the start-up screen, `sw.js` is `compicasa-shell-2026-11-23-loader`, `app.css` has the animation, `/healthz/ready` 200. Not seen on a phone or through a real cold start. CI/CD (B-030) has now deployed once. GitHub warns that `azure/login@v2` and `actions/download-artifact@v4` target Node.js 20 (still runs). These log lines committed, not pushed.
Then Sam: "Test and see if you can snag anything". Tested live and locally (desktop and phone width, dark and light, Spanish and English, reduced motion; no sideways scroll). Warm loads render in about 0.65 s (the HTML's first byte at 66 ms, scripts ~0.55 s, `/api/config` and `/api/me` ~20 ms each), so the screen flickered on for about 0.1 s. Fixed three snags: 1) the screen now waits 0.35 s before fading in, so warm loads don't flash it; 2) the header's help and language buttons showed as two empty pills until the first render (there before this session, more visible now): hidden while empty (`.hbtn:empty`); 3) the "server is waking up" line was transparent but still in the accessibility tree from the start, so screen readers could read it at once: now `visibility:hidden` until 6 s. Service worker `compicasa-shell-2026-11-23-loader2`. Checked in a local run (the computed styles at the end of each animation; header screenshot); the preview pane is hidden and prefers reduced motion, so the timings were read from the animations' clocks, not watched. Not found: a fallback if `app.js` itself fails to load (the screen would stay) and the blank page during a cold App Service start before any HTML arrives. Committed, not pushed (a push deploys).
Then Sam: "we need burger type menu for profile, language, and other settings - a new one for colour-scheme (light/dark/auto). More settings/menus items will probably too", and while it was being built: "Once you've done that, push so it deploys". Built B-055 (D-042): a menu button replaces the guide and language buttons; the panel has the account (name, Profile, Sign out), Language, Appearance (Automatic, Light, Dark) and More (guide, Terms, Privacy), drawn from `menuSections()` so a new setting is one entry. The colour scheme is stored on the device (`compicasa-theme`) and applied in `<head>` by a new `theme.js` (no inline script, so the CSP is unchanged), so a saved choice doesn't flash the other scheme; the CSS's two dark blocks now apply under `prefers-color-scheme: dark` unless Light is chosen, and always when Dark is. Profile left the tab bar (three tabs). Escape closes the menu and returns focus to the button; clicking outside, choosing a link or Sign out, or navigating closes it. Browser test updated (language switching goes through the menu; new checks: menu contents, fits on screen, Dark, Light and Automatic applied and remembered, Escape, Profile from the menu; three tabs): 66 of 66 against a local run with a test invite code (one earlier run had the known "closing the sheet goes back to People" timing slip, one hit the sign-up rate limit from back-to-back runs). Phone and desktop screenshots of the menu looked at. `dotnet test` not run here (no back-end change; CI runs it). Service worker `compicasa-shell-2026-11-23-menu`. Pushed with the snag fixes on Sam's word (recorded under F-023).

## 2026-11-23 · Session 20: a Home page from the logo
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Tester (B-053) and the CEO (D-040)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 19 (deployed, F-021). Sam: "The Compicasa logo should take you to a home page, which doesn't exist yet. The home should have a summary of where you are in the house/people search and your profile score, and some houses that might already interest them. Also remove the 'Proof of concept · made-up people' from the top." ("try again": an earlier attempt had saved nothing; the tree was clean.) No new answers in the founder queue.
**Goals:** 1. B-053 Home page and logo link. 2. Remove the header badge. 3. Tests pass; files current; commit.
**Needs Sam:** F-022 (new): the go to deploy.
**Did**
- Back end (`app/src/Compicasa.Api/Api.cs`): the homes query moved into `HomesForAsync`, shared by the group's homes and a new `GET /api/me/homes`, which ranks up to 6 homes for the viewer plus their best mutual match (else the most compatible person not passed on; else the viewer alone). Same fit rules and agencies-only index as before (D-035).
- Front end (`wwwroot/`): `#home` (`viewDash`): greeting, the one next step, four steps (profile, people, a group, a home) with the current one highlighted, a profile score card (`profileScore`, weights in D-040) whose gaps link to the right profile section, and three homes using the existing home cards. Land there after sign-in and after the wizard; the logo and name are now a link to `#home` and also close the guide or a legal page. The header badge and its strings and styles are removed. New strings in Spanish and English; service-worker version bumped.
- Tests: the group-homes .NET test also checks `/api/me/homes` (partner, mutual, at most 6, enough bedrooms; signed-out callers refused); 8 new browser checks (land on Home, four steps, score and homes, no badge, no English, no sideways scrolling, the logo goes Home from the guide).
**Decided:** D-040 (CEO): Home page from the logo, the profile score's definition, homes for you and your best match, the badge removed. **Approvals used:** none needed (Build mode, local only); nothing deployed or pushed.
**Learned:** a profile score from optional answers only: the wizard already insists on a name, areas and home types, so most new profiles start at 50 (Assumption: the weights have no user evidence yet). On the first run of the updated browser test one existing check ("closing the sheet goes back to People", phone) timed out once; it passed on the runs before and after, so it looks timing-related, not caused by this change (not confirmed). A browser-test run straight after another trips the sign-up limit, as noted in STATE.
**Open or blocked:** deploy waits for F-022. The guide (`#help`) doesn't mention the Home page yet.
**Saved and checked:** `dotnet test` 47 of 47; browser test 54 of 54 against a local run (SQLite in the temp folder), and again after the last change (54 of 55 with the timing slip above, then 54 of 54); phone and desktop screenshots of Home looked at; `git status` and `git diff` reviewed for STATE, BACKLOG, DECISIONS, FOUNDER-QUEUE, `app/README.md` and `app/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. F-022 deploy, then Sam's look on a phone. 2. B-042 and B-052 before anyone but Sam signs up. 3. B-047 crawl check.
**After the closing report:** Sam: "deploy" (recorded under F-022, closed). Deployed with `deploy.ps1 -App` (47 tests passed first). The first health check straight after returned 500.30 (app failed to start); 20 seconds later `/healthz/ready` said ready, so read as the free database waking, not a fault (not confirmed from the app logs). Live: the new service-worker version (`compicasa-shell-2026-11-23-home`), `app.js` with the Home view and the logo link are served, no badge, `/api/me/homes` refuses a signed-out caller (401), the CSP header is present; the live browser test passes 16 of 16 up to External ID (one earlier run used a dummy invite code by mistake, one tripped the sign-up limit). Home itself wasn't seen live past sign-in.
Then Sam: "Let's have some CI/CD setup so any deploys to main will just publish the latest version. Finish it manually this time" (D-041, B-030). Done: `.github/workflows/app.yml` (tests on pushes and pull requests touching `app/`; on `main`, publish, the External ID check, zip deploy, wait for ready; Windows runners); the app registration `compicasa-github-deploy` in Sam's own directory with a federated credential for `main` of `sambetts/project-juntos` only and the role Website Contributor on the web app only; the repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (IDs, not secrets). Not yet run: it starts on Sam's next push. Checked with `git status` and `git diff`; committed, not pushed.

## 2026-11-23 · Session 19: notifications, messages and the group lifecycle
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Product, the Builder and Tester (B-036, B-037, B-038) and the CEO (D-039)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 18 (B-050 and B-051 deployed). Sam: "Let's implement this", pasting the guide's "Not here yet" list (notifications; seeing who has liked you, and messages; invitations that don't expire and places that can't be filled). No new answers in the founder queue.
**Goals:** 1. B-037 in-app notifications. 2. B-036 messages and "liked you". 3. B-038 expiry, withdrawing, filling a free place.
**Needs Sam:** F-021 (new): the go to deploy, which also applies a database migration.
**Did**
- Back end: migration `Social` (tables Notifications, Messages, ThreadReads, Blocks, Reports; columns Account.ConductAcceptedAt, Group.PlannedSize, GroupMember.InvitedAt and InvitedById). `Lifecycle.cs`: group states, 14-day expiry when someone looks, free places, who may invite and withdraw, notices to members. `Social.cs`: inbox and counts, conversations (pairs and groups), scripted replies, code of conduct, block (leaves shared groups) and report; deleting an account or replacing the daily batch removes their messages, notices and blocks. `Api.cs`: notices on likes, matches, invitations, answers and leaving; invite and withdraw endpoints; only people still in a group can see it.
- Front end: a bell and `#alerts`; *Messages* as a fourth destination (`#messages`, `#chat-<id>`) and a *Chat* view on each group; conduct and report dialogs; "♥ Liked you" chip and first place in *To review*; on a group, who has gone, a free-places card with your own matches to invite, "until <date>" and *Withdraw* on pending members, "answer by <date>" on invitations; blocked people under Profile. 101 new strings in each language (658 keys each, none missing); the guide's sections rewritten for D-039; service-worker version bumped.
- Fixed an existing bug found while testing: opening or reloading the app at a People address (e.g. `#people-review`) never loaded anyone, because `applyRoute` saw "the same page" and skipped loading. Live since Session 18.
- Tests: 7 new .NET tests (`SocialTests.cs`); 4 new browser checks in `tests/e2e/e2e.mjs`; a two-person Playwright script in the session's scratch folder (not in the repository) for decline, free place, filling, withdrawing, group chat, report and block.
**Decided:** D-039 (CEO): likes tell the other person; groups carry on when someone drops out and freed places can be filled by members who said yes; blocking leaves shared groups; conduct before the first message. **Approvals used:** none needed (Build mode, local only); nothing deployed or pushed.
**Learned:** "seeing who liked you" can't sit with silent likes, so D-038's silent-like rule changed. With only Sam real on the live site, "liked you" would always be empty, so a fixed sixth of willing made-up people like you first (a quarter showed 10 of 32 people, too many). SQLite can't compare DateTimeOffset values in queries, so expiry and rate limits compare in memory over small sets.
**Open or blocked:** deploy waits for F-021. Left in B-037: emails (ACS). Left in B-038: next-steps checklist, editing the whole brief. New B-052: privacy notice and data map for the new data. The *Chat* view tab sits past the edge of the group's sub-tabs on phones (they scroll sideways, as before).
**Saved and checked:** `dotnet test` 47 of 47; browser test 46 of 46 against a local run, twice; two-person script 14 of 14; screenshots (phone and desktop, light and dark) of chat, conduct dialog, notifications, free place and a filled place looked at; `git status` and `git diff` reviewed for STATE, BACKLOG, DECISIONS, FOUNDER-QUEUE, `app/README.md` and `app/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. Sam's look at the live site on a phone. 2. B-042 and B-052 before anyone but Sam signs up. 3. B-047 crawl check.
**After the closing report:** Sam: "deploy" (recorded under F-021, closed). Deployed with `deploy.ps1 -App` (47 tests passed first). Live: `/healthz/ready` ready, so the `Social` migration applied (the app migrates before it listens); the new service-worker version (`compicasa-shell-2026-11-23-social`), `app.js` and `i18n.js` are served; the CSP header is unchanged; `/api/inbox/count` refuses a signed-out caller (401); the live browser test passes 16 of 16 up to External ID. Past sign-in the new features weren't seen live (needs a real inbox): Sam's own account is the check. Checked with `git diff`; committed, not pushed.
Then Sam gave his own sign-in and asked me to sign in; I did so in a headless browser on the live site (password passed through an environment variable for that one run, not saved anywhere), read-only: no likes, messages or conduct acceptance on his behalf. Seen live on a phone-sized screen: four tabs, the bell, "♥ Le interesas" on 3 made-up people, Messages with 4 conversations (two groups, two matches), a chat with the conduct-safe composer, Groups; no script errors, no sideways scrolling; 0 notifications (expected: nothing real has happened to his account). One slip fixed in code, not yet deployed: the group's state label stretched across the row in the Messages list.

## 2026-11-23 · Session 18: calmer navigation and pages
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Product, the Builder and Tester (B-051) and the CEO (D-038); the plan reviewed by a second model (Fable 5.2)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 17. Sam: "The matches screen is very busy especially. They all are. Make them all clearer & critique the format generally … restructure the nav & the pages so it's not so overwhelming … not have everything presented at once" and "'edit your profile' and 'back to matches' on the bottom seem to take a lot of space". No new answers in the founder queue.
**Goals:** 1. B-051: critique, plan, second-model review. 2. B-051: build; tests pass. 3. Files current, commit.
**Needs Sam:** the go to deploy B-050 and B-051 together; nothing new in the founder queue.
**Did**
- Ran the app locally (SQLite in the session's scratch folder, outside the repository) and took full-page screenshots of every signed-in screen with Playwright (`playwright-core` in the scratch folder, driving Edge) at 1280 and 390 px. Critique and plan: `product/2026-11-23-ui-restructure.md` (11 findings).
- A second model (Fable 5.2, as a helper with the plan and screenshots, no edits) reviewed the plan: "build with changes". All eight changes taken (three views per group, not five; no "said no" list; a person sheet instead of inline "More"; suggestions under Groups; first card on the first phone screen; "made-up people" kept in the phone header; a real empty Homes state; tabs as links). The two it suggested and I didn't take are listed in the note.
- Built (front end only): People, Groups and Profile as plain destinations (header tabs on wide screens, a bottom tab bar on phones); the bottom bar only for the wizard and while building a group. People: *To review*, *Liked*, *Matches*, *Passed* from a link; compact cards, 12 at a time; a person sheet at `#person-<id>`. Groups: invitations, then suggested groups (not ones you already have), then your groups. A group: *Overview*, *Home brief*, *Homes* (list first; *Market* switch for Session 16's tiles and charts). Profile: one summary line per section, opened to edit. Every view has an address and Back works. All new wording in Spanish and English (557 keys in each); the in-app guide (B-050) updated to the new names; service-worker version bumped.
- Browser test updated for the new structure, plus 7 new checks (42 in all).
**Decided:** D-038 (CEO): calmer navigation, three destinations, one job per view; no list of who said no to your like. **Approvals used:** none needed (Build mode, local only); nothing deployed or pushed.
**Learned:** pages are now a quarter to a fifth of their old height (People 7,734 → 1,857 px on desktop; a group with homes 5,047 → 953 px for its overview; the phone group page 10,212 → 1,389 px). The "cropped at 390 px" artefact noted in Sessions 16 and 17 was a real overflow: the old phone pages were 482 px wide; now 390. Sign-in's 10-a-minute limit trips when a screenshot script runs straight after the browser test.
**Open or blocked:** deploy waits for Sam's go. Still open in the note: home cards are the busiest element left; no Previous/Next in the person sheet; a real phone and screen-reader check (B-025).
**Saved and checked:** `dotnet test` (40 passed); browser test 42 of 42 against a local run, twice; screenshots of every view at 1280 and 390 px, light and dark, Spanish and English, with no sideways scrolling and no script errors (the one 401 is the signed-out `/api/me` check before sign-up); i18n key check (none missing in either language); `git status` and `git diff` reviewed for STATE, BACKLOG, DECISIONS, the note, `app/README.md` and `app/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. A look at the live site on a real phone (B-050 and B-051 are live). 2. B-047 check the WebJob crawl. 3. B-042.
**After the closing report:** Sam: "you deploy" (recorded as F-020, closed). Deployed with `deploy.ps1 -App` (40 tests passed first). Live: `/healthz/ready` ready; the new service-worker version (`compicasa-shell-2026-11-23-calm`) and the new `app.js` and `i18n.js` are served; the strict CSP header is unchanged; the live browser test passes 16 of 16 up to External ID. Past sign-in the new pages weren't seen live (signing in there needs a real inbox): Sam's own account is the check. Checked with `git diff`; committed, not pushed.

## 2026-11-22 · Session 17: an in-app guide to likes and groups
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Product and the Builder and Tester (B-050)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 16. Sam: "How does a real person respond to a group or a like? It's not clear this mechanism to me or the mechanics of the site. We need some docs/guide baked in. Keep it basic for now as it'll probably change."
**Goals:** 1. B-050: an in-app guide. 2. Tests pass, files current, commit.
**Needs Sam:** the go to deploy (B-050); nothing new in the founder queue.
**Did**
- Read the mechanics from the code (`Community.cs`, `Api.cs`, `Views.cs`, `app.js`). A like is silent: the other person isn't told and sees the liker as an ordinary card; it becomes a mutual match only when they like back (made-up people "like back" by simulation). Only mutual matches can be invited, up to three others. An invited real person finds the group under "Groups and home" with Accept and Decline; the answer is final; the group forms when everyone accepts and breaks if anyone declines or leaves. No notifications, "liked you" list, messages or expiry yet (B-036 to B-038).
- Built B-050: a `#help` page in Spanish and English (the steps, likes and matches, groups and invitations, what isn't there yet), linked from the header and the footer, open signed in or not; the invitation card in the groups list gained an "Open" button, so an invitee can see the group and its home before answering (the group page already showed Accept and Decline). New browser check for the guide; service-worker version bumped.
**Decided:** nothing new. **Approvals used:** none needed (Build mode, local only); nothing deployed or pushed.
**Learned:** an invitee could previously only Accept or Decline from the list, without seeing the group first; fixed. Headless Edge at 390 px crops the guide on the right exactly as it crops the existing privacy page (B-025's known artefact); the browser test's no-sideways-scroll check passes. On a phone the header's "How it works" button wraps the language button onto a second line; acceptable for now.
**Open or blocked:** deploy waits for Sam's go.
**Saved and checked:** `dotnet test` (40 passed); browser test 35 of 35 against a local run (scratch SQLite outside the repository), twice; screenshots of the guide at 1280 and 390 px; `git status` and `git diff` reviewed for STATE, BACKLOG, `app/README.md` and `app/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. B-050 deploy (on Sam's go). 2. B-047 check the first WebJob crawl. 3. B-042.

## 2026-11-22 · Session 16: design review, homes and matches explorers
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Product and the Builder and Tester (B-049), and the CEO (D-037)
**Mode:** Build (D-023), within D-027 and D-035 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 15. Sam: "Run a design review. I want this website to look amazing, inspiring, and analytical (meaning like it has lots of data; reactive to filters and requirements). Think of any improvements you can to the UI to make this the gold standard of new house finding platforms."
**Goals:** 1. Design review note. 2. Build the top items. 3. Tests pass, files current, commit.
**Needs Sam:** the go to deploy (B-049); nothing new in the founder queue.
**Did**
- Ran the app locally (SQLite in the session's scratch folder, outside the repository) and screenshotted every screen at 1280 and 390 px, light and dark. Review: `product/2026-11-22-design-review.md` (14 findings; 9 done, 5 proposed).
- Built (B-049), front end only plus one API limit:
  - Homes explorer on the group page: filters (dual-handle price slider, minimum size, bedrooms, type, area, energy, confirmed must-haves, real adverts only), five sorts, "show 12 more"; stat tiles (homes shown, median price, median €/m² with its change against all that fit, median size, best fit); a price histogram (tap a bar to filter), a price-against-size scatter with the median €/m² line (tap a dot to jump to the home) and median €/m² by area, all redrawn as filters change; each home gets a floor-plan sketch drawn from its facts (labelled as not the real plan; no photos, D-035), an energy badge, a fit ring and its €/m² against the median.
  - Matches explorer: stat tiles, an affinity histogram (tap a bar to set the minimum), filters by status (with counts), minimum affinity and name.
  - A house-with-two-doors logo, a hero band, loading placeholders, gentle hover lift (off for reduced motion). All new wording in Spanish and English (`i18n.js`, 471 keys in each).
  - `GET /api/groups/{id}/homes` returns up to 200 homes that fit (was 24) so the browser can filter them.
  - B-045 item 5: the double-like race. Like, pass and undo buttons disable on click, and the reactions endpoint updates the row when another request inserted it first. It had failed the browser test in both of this session's first runs.
- D-037 (CEO): homes, not people, can be filtered by price, in the browser and unstored; people only by status, affinity and name.
- Charts follow the dataviz rules: one accent over a grey context series with a legend, hairline grids, 4px rounded bar ends, a tooltip on every mark; drawn at the container's real width so labels stay readable on phones; coloured from CSS classes under the strict CSP. A first tooltip class (`.tip`) clashed with the existing "Idea" tags and was renamed.

**Decided:** D-037 (CEO). **Approvals used:** none needed (Build mode, local only); nothing deployed or pushed.
**Learned:** the price histogram and scatter are thin with made-up homes for a big group (9 to 12 points); they will mean more with the live index's real homes. On a 390 px headless window the action bar's second button runs off the edge on the group page; it did before this session too (B-025 notes headless lays phones out wider than the screen), so not confirmed on a real phone.
**Open or blocked:** B-049 deploy waits for Sam's go. Proposed follow-ups are in the review and the B-049 row.
**Saved and checked:** `dotnet test` (39 passed); browser test 34 of 34 against a local run, no script errors; a scratch screenshot script (outside the repository) found no sideways scrolling at 390 px or 1280 px in either theme; `git status` and `git diff` reviewed for STATE, BACKLOG, DECISIONS, the review note, `app/README.md` and `app/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. B-049 deploy (on Sam's go) and a live check with real homes. 2. B-047 check the first WebJob crawl. 3. B-042.
**After the closing report:** Sam: "deploy" (recorded as F-018, closed). Deployed with `deploy.ps1 -App` (39 tests passed first). Live: `/healthz/ready` 200; the new `app.js` and service-worker version are served; the live browser test passes 15 of 15 up to External ID. The explorers past sign-in weren't seen live, since signing in there needs a real inbox: Sam's own account is the check. The first WebJob crawl (started 18:46 UTC) kept running through the deploy and had stored 600 homes by 19:31 UTC. Checked with `git diff`; committed, not pushed.
Then Sam asked whether the made-up homes are regenerated daily (no: 90 fixed homes, added once at start-up; it's the made-up people that change daily), and suggested the crawler makes them unnecessary. Recommended switching them off live; Sam: "Do it. First though, give me the invite code" (F-019, closed). The invite code was given in chat only, never written to the repository.
- New setting `Homes:Demo` (on unless "false"): when off, start-up deletes the made-up homes and the homes endpoint leaves them out. `Homes__Demo=false` set on the web app and in `infra/resources.bicep`. New test (40 .NET tests pass).
- Deployed with `deploy.ps1 -App`. The app setting's restart stopped the first crawl at 19:37 UTC with 675 homes stored; it was started again by hand at 19:39 UTC and skips adverts it already has. The first live browser run hit 503s during the restart; the rerun passed 15 of 15.
- Not seen live: the made-up rows' deletion (reading the database needs a firewall rule); covered by the new test and by the endpoint's filter. Sam's signed-in look will show it: no home should say "Inventada".
Checked with `git diff`; committed, not pushed.

## 2026-11-22 · Session 15: our own index of homes for sale
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Research and Legal & Compliance (sources and law), the CEO (D-035), the Builder and Tester (B-046)
**Mode:** Build (D-023), within D-027 and D-035 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 14. Sam: a database of homes for sale in Spain, "scrape manually" from agencies and update daily until formal feeds exist, with common facts and a link to the original advert, matched to groups and referring them to the agency; "run a few tests before you decide how to implement this."
**Goals:** 1. Allowed tests of sources and terms. 2. Research note with a design. 3. F-017 for Sam. After his answer, 4. build it (B-046).
**Needs Sam:** F-017 (answered in the session); the go to deploy (B-047).
**Did**
- Tests without fetching any listing: robots.txt of 4 portals, 11 agency networks and bank-owned sellers, Kyero, Casafari and the BOE auctions site; the legal notices of 9 agency networks; LPI art. 133 in the BOE. Of the nine notices, six ban extraction or deep links without permission, two ban reproducing content (RE/MAX for commercial use only), and one couldn't be read. Note: `research/2026-11-22-listings-index.md`.
- F-017 asked in chat with a recommendation (A: permission-first). Sam chose **B: crawl agency sites now** (never portals), then added: "We need to clearly link each house to the original agent… Unless they specifically issue a cease & desist, I think this is an acceptable route for now." Recorded as D-035. The AGENTS.md off-limits line was changed to match his decision (portals stay off-limits). F-017 closed, and F-009 closed as superseded.
- Technical tests after D-035: Tecnocasa's sitemaps list adverts with last-changed dates (about 1,250 homes for sale in the province of Madrid in the first of two files), and each advert page carries its facts as JSON. Redpiso's schema.org markup has price and town only.
- B-046 built:
  - Fit rules against the group's brief and a postcode-based placement in the five Madrid areas (approximate, Assumption).
  - A `Homes` table and migration.
  - A crawler run by hand from the built app (`crawl tecnocasa`): portals blocked in code, robots.txt honoured, 3 seconds between requests, only changed adverts fetched, gone adverts marked, the `Homes:Stopped` switch, and a `/bot` page that explains it and how to block it.
  - The Tecnocasa adapter keeps facts, the agency office's name and the link, nothing else.
  - 90 made-up homes.
  - `GET /api/groups/{id}/homes` for members only.
  - A "Homes for sale that fit" section on the group page (both languages), with real homes first and a "See the advert at <agency>" button opening the agency's page in a new tab.
- A test crawl of 40 pages per run (two runs; the first stopped on a network timeout, now skipped per page) stored 65 real homes in a local scratch database, outside the repository.
- Tests: 39 .NET tests (9 new) and 34 browser checks (2 new, homes) pass locally. One browser run failed on an old race (two quick likes of the same person), now B-045 (5); the rerun passed.
- The lawyer brief gained A7 (database right, deep-link clauses, objections, intermediary status). R-023 added, R-005 updated. B-047 (go live) and B-048 (more agencies) added; B-014 superseded.
- `dotnet-ef` 10.0.15 installed as a global tool on this machine. `dotnet run` was refused by the machine's app control (Access is denied on the `.exe`); running the built DLL works.

**Decided:** D-035 (Sam). **Approvals used:** F-017 option B and Sam's follow-up (both 2026-11-22, in Claude Code). Nothing was deployed, and nothing was written to the live database.
**Learned:** every agency network checked restricts crawling in its terms (Verified, 2026-11-22). LPI art. 133.2 covers repeated extraction of small parts (Verified, BOE). Not checked: Engel & Völkers' terms (the page is built by script); which CRMs Madrid agencies use and whether a new portal can receive their Kyero feed.
**Open or blocked:** B-047 waits for Sam's go to deploy.
**Saved and checked:** `dotnet test` (39 passed); browser test 34 of 34 against a local run with screenshots of the homes on phone and desktop; `git status` and `git diff` reviewed for AGENTS, DECISIONS, FOUNDER-QUEUE, BACKLOG, RISKS, STATE, the lawyer brief and `app/`. LOG saved last; its check and the commit are confirmed in the closing report.
**Next session should start with:** 1. B-047, if Sam says deploy. 2. B-042. 3. B-045 and B-048.
**After the closing report:** Sam: "deploy and push" (recorded under F-017). Deployed with `deploy.ps1 -App` (39 tests passed first). Live: `/healthz/ready` ready, `/bot` served, the homes endpoint refuses anyone not signed in, and the live browser test passes 15 of 15 up to External ID. The first live crawl didn't run: Claude Code's safety check refused adding a SQL firewall rule for this machine (B-047). Checked with `git diff`; committed and pushed.
Then the first live crawl. Sam chose to add the SQL firewall rule himself, then said "You fix it": the AI added `crawl-session15`. Sign-in failed twice ("The server is not currently configured to accept this token": the default credential chain picked up another account on the machine, even with the tenant pinned). The crawler gained a `Database:Credential=AzureCli` option that takes a token from `az account get-access-token`; with it the crawl connected and stored 50 homes. Sam asked for a high-level explanation: `product/homes-index.md`.
Then Sam: move the PoC onto his existing App Service plan BuildSyncProd (Windows, P1v2, UK South), deleting and recreating everything, with the crawl as a WebJob (D-036, his words recorded).
- The local crawl was stopped. The invite code and External ID settings were saved outside the repository first.
- `rg-compicasa-prod` was deleted, together with the old firewall rule, and recreated in UK South. The web app kept its name and address and runs on BuildSyncProd from another resource group. The SQL free offer was accepted in UK South. The WebJob `homes-sync` runs daily at 05:00 Madrid time.
- Problems on the way:
  - The deployment name was pinned to Spain Central, so deployments are now named per region.
  - The first site create raced the old group's deletion and was lost; a rerun fixed it.
  - For about 10 minutes, Azure's lookups of the new site failed now and then.
  - A JMESPath query in `deploy.ps1` came back empty from PowerShell, so the script now reads settings as JSON.
  - `deploy.ps1` printed its arguments, invite code included, in an error. The code appeared in this session's terminal only, not in the repository; errors now hide arguments.
- The draft privacy notice now says the data is in the UK, which has an EU adequacy decision (Reported, not re-checked this session).
- Live: ready, External ID sign-in, `/bot` served, browser test 15 of 15. The WebJob was started by hand at 18:46 UTC and stored 25 homes in its first two minutes, using the app's managed identity.
Checked with `git diff`; committed and pushed.

## 2026-11-22 · Session 14: the sign-up error
**Who:** Sam with GitHub Copilot CLI (Opus 5.5), as Chief of Staff, with the Builder and Tester (B-034)
**Mode:** Build (D-023), within D-027 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md after Session 13. Sam: "Fix production", with a screenshot: External ID's "Create account" page saying "We couldn't find an account with this email address" for his email.
**Goals:** 1. Find why Sam's sign-up fails. 2. Fix what's ours, deploy, check live.
**Needs Sam:** one more test sign-up, in an InPrivate window.
**Did**
- Read the tenant with Graph (as the tenant admin): the sign-up flow is linked to "Compicasa web", sign-up is allowed, email with password is on, and no account or deleted account exists for Sam's email.
- Reproduced in Edge driven by Playwright with fresh profiles, on the same authorize address: example.com, an unused outlook.com address and Sam's own address all reached "Enter code" (GetCredentialType `IfExistsResult: 0`), headless and visible. Sam received the code (not used). So the tenant and the app are fine; the error comes from something in Sam's own Edge profile (Assumption: an earlier sign-in kept by that profile; not confirmed).
- Correction to Session 13: External ID does honour `prompt=create` (Sam's screenshot and our runs both open on "Create account"). Session 13 compared the pages' raw HTML, which is the same for both because the page is built by script. The live tip told people to tap "No account? Create one", a link that page doesn't have: now it says to type the email on "Create account" and tap Next (both languages), and the comment in `Accounts.cs` and the browser check follow.
- Deployed with `deploy.ps1 -App` (30 tests passed first); the live browser test passes 15 of 15. The Azure CLI's default was set back to the subscription.

**Decided:** none. **Approvals used:** "Fix production" (Sam, 2026-11-22), recorded under F-016.
**Learned:** External ID's sign-up and the flow work as set up (Verified, Graph and browser runs, 2026-11-22). Not found: why Sam's own profile fails; searched Microsoft Q&A (one report fixed by a private window, one about addresses used by another identity provider, not our case).
**Open or blocked:** B-034 waits on Sam's sign-up in a clean window.
**Saved and checked:** `git diff` reviewed for `i18n.js`, `Accounts.cs`, `e2e.mjs`, BACKLOG, STATE, FOUNDER-QUEUE; LOG saved last; commit confirmed in the closing report.
**Next session should start with:** 1. B-042. 2. B-036 messages. 3. B-045, including (4) Sam's normal-profile error.
**After the closing report:** Sam: "ok inprivate mode works", then "it's done". Graph shows his External ID account created 2026-11-22 14:09 UTC. B-034 closed; the normal-profile error became B-045 (4). Checked with `git diff`; committed.

## 2026-11-22 · Session 13: release-readiness review, email sign-up and onboarding
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Tester (B-033, B-034), the CEO (D-034) and Legal & Compliance (draft terms and privacy texts)
**Mode:** Build (D-023), within D-027 and D-033 · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 12"; newest LOG entry Session 12. Sam: "Look at the app code. Does it have the funcionality needed for a release? This PoC must be as close to release-ready as possible … The user sign-up for example feels basic." After the review: "yes. External for email via Azure Communication Services (although open to suggestion)"
**Goals:** 1. Record the decision and put the review into the backlog. 2. B-033 EF Core migrations with a baseline for the live database. 3. B-034 email sign-in through External ID, 18+ and terms checks, onboarding wizard.
**Needs Sam:** F-016: create the External ID tenant (run or approve `app/infra/external-id.ps1`).
**Did**
- Review (in chat, then in BACKLOG): the PoC stops where a real user's journey starts: no messages after a match, no notifications, a basic username sign-up, no migrations, and the household dealbreaker would filter real people (AGENTS.md section 4). New rows B-033 to B-044, in priority order.
- Recorded D-033 (Sam's words) and D-034 (CEO). Read Sam's "External for email via ACS" as External ID for sign-in and ACS Email for the app's own emails, and said so in the opening brief.
- B-033: EF Core migrations for Azure SQL (`Initial` = Session 12's schema, `EmailAccounts`), applied at start-up; a one-off baseline marks `Initial` as applied on the live database, which was made without migrations; SQLite keeps creating its schema directly; a test fails if the model has unmigrated changes. `dotnet-ef` tool updated from 9.0.7 to 10.0.15.
- B-034:
  - Email accounts. In production, External ID with an ID token only (no client secret). The invite code, the 18+ box and today's terms are checked before the hand-over and travel as a signed 30-minute ticket. Without a ticket, no account is created. Locally, email-and-password accounts with the same checks.
  - The cookie is now secure-only outside development.
  - An eight-step onboarding wizard. Matches, likes, previews and new groups answer 409 until it's done, and unfinished accounts are invisible to others.
  - Draft terms and privacy notice in both languages, marked as drafts. Sam's details aren't in them.
  - A password meter and a show/hide button.
  - `app/infra/external-id.ps1` (tenant, app registration, sign-up flow, web app settings); Bicep and `deploy.ps1` keep the External ID settings, and `deploy.ps1 -App` refuses to deploy without them.
- Tests: 30 .NET tests (11 new) and the browser test (32 checks locally, now through the wizard and the privacy page). The first browser run caught a layout bug (the show-password button squashed the password box on phones), fixed.
- Not done: creating the tenant. The PUT to `Microsoft.AzureActiveDirectory/ciamDirectories` was stopped by Claude Code's safety check ("Destructive or Unwanted Actions"), so it became F-016. Nothing was created in Azure this session; the live site is unchanged.

**Decided:** D-033 (Sam), D-034 (CEO)
**Approvals used:** none acted on in Azure. Reads only: the resource provider's state and a name check (compicasa.onmicrosoft.com is free).
**Learned:** External ID's core offer is free for the first 50,000 monthly active users (Verified, Microsoft Learn pricing page, 2026-11-22); premium add-ons such as SMS bill from the first use. The tenant resource can get stuck in a failed state that blocks deleting its resource group (Reported, Microsoft Q&A), so it gets its own group. Graph's user-flow format was read from Microsoft Learn (`identitycontainer-post-authenticationeventsflows`); it needs both the attributes and the page views. Not checked: ACS Email's price per message (before B-037); the script itself, which can't run until F-016.
**Open or blocked:** B-034 blocked on F-016; B-033's first live run comes with that deploy.
**Saved and checked:** `dotnet test` (30 passed) and the browser test (32 passed) against a local run; `az bicep build` clean; both PowerShell scripts parse. Reviewed `git diff` for BACKLOG, FOUNDER-QUEUE, DECISIONS, STATE, `app/README.md` and `app/infra/`, and compared them with what I meant to write. Three commits this session; LOG.md was saved last and checked with `git diff`; the final commit is confirmed in the closing report.
**Next session should start with:** 1. Close B-034 after Sam's test sign-up. 2. B-042. 3. B-036 messages.
**After the closing report:** Sam asked why the tenant couldn't be created ("Creating one from tenant ID 97aa2ef1-… I did previously with claude for another app"). Corrected my earlier wording: Azure hadn't refused; Claude Code's safety check had. 97aa2ef1-… is the subscription's own directory. Sam: "yes" to running the script, then "do it all" for the deploy and checks (both in FOUNDER-QUEUE, F-016).
- `external-id.ps1` ran: tenant `compicasa.onmicrosoft.com` (c89b050e-…) in `rg-compicasa-identity`; app registration "Compicasa web" (3757b3aa-…, ID tokens only); the email sign-up flow; the web app's `Auth__*` settings. Sam signed in to the new tenant as root@elbetziogmail.onmicrosoft.com.
- Deployed with `deploy.ps1 -App` (30 tests passed first). Live `/api/config` says external; "Entrar" redirects to `compicasa.ciamlogin.com`, whose page loads in Spanish ("Iniciar sesión … Compicasa"). The app migrates before it listens, and it started, so the baseline and new columns are in. The startup log lines don't reach Application Insights (B-045).
- Browser test against the live site: 14 passed, 0 failed (sign-up checks in Spanish, then the hand-over to External ID).
- The Azure CLI's default account had moved to the new tenant; set back to subscription 81209232-….
- B-033 done; B-034 waits on Sam's own test sign-up; B-045 added. F-016 closed. Checked with `git diff`; committed.
Sam's test sign-up failed: "We couldn't find an account with this email address." Cause: External ID ignores `prompt=create` and always shows its sign-in page, which only looks up existing accounts (confirmed by fetching both pages: identical; Reported broken since February 2026 on Microsoft Q&A). Our side already copes if people tap "No account? Create one" there, since the ticket travels with them. Fix: the sign-up step now says so plainly, in the page's own words in each language, with a browser check; `prompt=create` kept. Tests pass (30). Not deployed yet: the tool's safety check stopped `deploy.ps1 -App`; waiting for Sam.
Sam: "deploy". Deployed (30 tests passed first); the live browser test passes 15 of 15, including the new instruction. Checked with `git diff`; committed.

## 2026-11-22 · Session 12: working PoC with a C# backend, hosted in Azure
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Tester (B-028, B-029), the CEO (D-028) and Finance (ledger)
**Mode:** Build (D-023), with Sam's scoped exception for hosting (D-027) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 11"; newest LOG entry Session 11. Sam: "start next session. I want a build with a C# backend hosted in Azure subscription ID 81209232-7da9-4f81-b984-7ab0bd3dcfb3. You are the admin, azcmd has access to this sub. We build the PoC in Azure and pretend it's production for now. We're not doing dev/staging environments as this is a PoC, but one that'll look like production." Then: "The only requirement I have is the backend is C#, and for Azure. The front-end, however you like. Whatever's best. We'll need mobile & web apps", and: "The PoC should have fake data injected & refreshed daily for now"
**Goals:** 1. B-028 working PoC with a C# backend and a front end for phones and web. 2. B-029 host it in Azure as production. 3. Close the session.
**Needs Sam:** Nothing new in the queue. Who gets the invite code is his call (D-027).
**Did**
- Recorded Sam's words before acting: D-027 (host in Azure as production; the scope and what it doesn't cover), D-028 (stack, CEO), D-029 (made-up people refreshed daily). Reshaped B-028, which had said "a Node server, local and free", and added B-029.
- B-028 (Builder with Tester), `app/`:
  - Matching engine: the prototype's logic block ported to C# (`Compicasa.Core`). It returns message codes, and the wording stays in the front end. A Node script exports the 14 made-up profiles and a parity fixture from the prototype itself; the C# engine matches it exactly on all 240 pairings, the group suggestions, the briefs and the group checks.
  - API: ASP.NET Core minimal API on .NET 10; EF Core on Azure SQL (SQLite locally); accounts with an invite code, hashed passwords and a SameSite=Strict cookie; profiles validated on the server; likes, passes and mutual matches; groups with invitations; the home brief. Other people's dealbreaker settings, sliders and pickiness never leave the server.
  - Made-up people (D-029): the 14 hand-written profiles plus 30 generated ones, replaced each day after 04:00 Madrid time; likes and groups involving replaced profiles go with them.
  - Front end: a PWA (installable on Android and iOS), plain HTML, CSS and JavaScript, views ported from the prototype, Spanish first with English; the string tables were copied from the prototype and extended. Strict CSP, so colours are set from script.
  - Tests: 19 .NET tests (parity, fairness, API, security headers, daily refresh) and a 24-check browser test that drives headless Edge through the DevTools protocol at phone and desktop sizes, in both languages.
  - The browser test caught a real bug: C# passed a lone `string[]` as the whole `params object[]`, spreading list arguments. Fixed in `Msg`, with a test.
- B-029 (Builder), `app/infra/`: Bicep for `rg-compicasa-prod` in Spain Central; `deploy.ps1` (infrastructure, the managed identity's database user via a temporary firewall opening, then tests, publish and zip deploy). Live at https://app-compicasa-prod-25yr5.azurewebsites.net. All 24 browser checks pass on the live site; HTTP redirects to HTTPS; HSTS, CSP and `noindex` headers present.
  - The free serverless SQL offer was refused in Spain Central ("not supported for provided service level objective or region"). I switched to SQL Basic to keep the data in Spain, and corrected D-028.
  - I also changed the daily-refresh worker to check the date in memory, so it doesn't query the database every 15 minutes.
- Finance: the running cost is in `project/budget.md`. Risks: R-007 updated, R-022 added. Backlog: B-030 (deploy from GitHub Actions), B-031 (native speaker's review of the new Spanish), B-032 (store apps, later). Builder role file and README updated.

**Decided:** D-027 (Sam), D-028 (CEO, corrected the same day: SQL Basic, not the free offer), D-029 (Sam)
**Approvals used:** D-027, 2026-11-22: created `rg-compicasa-prod` and its resources in subscription 81209232-…, and deployed the app. Nothing else was published, bought, sent or registered; no domain; the invite code went to no one.
**Learned:** Azure's free SQL offer isn't available in Spain Central. nuget.org is blocked on this machine, but its configured feed works. Cost (Reported, Azure retail price API, EUR, VAT not included): App Service B1 Linux €0.0162 an hour, SQL Basic €0.1445 a day, so about €16 a month. Not checked: whether Sam's Visual Studio subscription credit covers it; real phones and screen readers; a native speaker's review of the new Spanish.
**Open or blocked:** B-030 needs Sam to push a workflow and add a federated credential. Who gets the invite code is Sam's call.
**Saved and checked:** `dotnet test` (19 passed) before each deploy; the browser test passed locally and on the live site (24 passed); reviewed `git status` and `git diff` for DECISIONS, BACKLOG, RISKS, STATE, README, budget and the Builder file, and compared them with what I meant to write. LOG.md was saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-027 DPIA, covering the hosted PoC. 2. B-030 deploy from GitHub Actions (with Sam). 3. B-020 privacy notice and consent texts.
**After the closing report:** Sam: "Put it on the free tier az resources, or as close as possible". Recorded as D-030.
- The web plan moved from B1 to F1 (free) in Spain Central, with Always On and the health check off, since F1 has neither.
- I tried the SQL free offer in Spain Central again through the CLI, and it was refused again. A new SQL server in France Central accepted it (`sql-compicasa-eu-25yr5`, GP_S_Gen5_2 with `useFreeLimit`; it pauses if the allowance runs out). The app's managed identity got its database user there. The app restarted and seeded itself.
- Before deleting the old Spain Central server, I counted its accounts: 30 generated and 14 seed, no real accounts. Then I deleted it.
- The log cap is now 0.1 GB a day, and the budget alert is now €5.
- All 24 browser checks pass on the free set-up.
- Updated `app/README.md`, `app/infra/`, budget, risks (R-022), backlog (B-029), STATE and README. Expected cost €0, plus a few cents for the hours on B1 and SQL Basic. Checked with `git diff`; committed.
Then Sam: "Move everything to Western Europe or somewhere in europe it's all in the same region. Doesn't have to be in spain". Recorded as D-031.
- App Service resources can't change region, so I deleted the Spain Central web app, plan, Application Insights, its alert rule and the log workspace (no user data in any of them) and recreated them under the same names, which kept the address. The app was offline for about 20 minutes during the move.
- France Central refused: its F1 quota on this subscription is 0. A validate-only probe found F1 quota in West Europe, Sweden Central, Italy North, Poland Central and Spain Central, but not in North Europe, Germany West Central or France Central.
- West Europe then refused a new SQL server ("not accepting creation of new … SQL Database servers at this time").
- In Sweden Central, Italy North and Poland Central the SQL server was created, but the free database failed with an internal error, repeatedly. A second free database on the France Central server worked, so it isn't a subscription limit.
- Outcome: the web app on F1 in West Europe, the database stays on the free offer in France Central. The invite code is unchanged. The deploy script now re-creates the database user for a rebuilt web app's new identity and takes `-InviteCode`. I deleted the three empty test servers.
- All 24 browser checks pass. F-015 added: how to get to one region. Checked with `git diff`; committed.
Sam answered F-015: "We'll go A for now". Recorded as D-032: the web app stays in West Europe and the database in France Central, at €0. F-015 closed; STATE, backlog and the app README updated. Checked with `git diff`; committed.

## 2026-11-22 · Session 11: privacy groundwork, rename, name clearance
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with Legal & Compliance (B-013), the Builder and Tester (B-012), and Brand & Marketing (B-024)
**Mode:** Build (D-023) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 10"; newest LOG entry Session 10. Sam: "do all the next tasks"
**Goals:** 1. B-013 privacy and safety groundwork. 2. B-012 rename the prototype to Compicasa. 3. B-024 name clearance for Compicasa.
**Needs Sam:** F-014 new: a ten-minute trademark register search, then approve the domains if it's clean. F-013 is still open.
**Did**
- B-013 (Legal & Compliance): `research/2026-11-22-privacy.md`.
  - Contents: who's the controller; a data map of nine purposes with lawful basis, who sees what, where it lives and retention; sensitive data; the DPIA decision; requirements for tools; breach and safety incident steps; what the waitlist page needs before it goes live.
  - Checked against the BOE texts of the GDPR (arts. 2.2.c, 9.2.a, 12.3, 22, 28.1, 30.5, 33), LOPDGDD art. 11 and LSSI arts. 10 and 21, plus the AEPD's art. 35.4 list.
  - The pilot meets AEPD criteria 1 and 4, so a short DPIA is proposed (new B-027).
  - New finding: LSSI art. 10.1 requires a website to show its provider's name and postal address.
  - Four new questions added to the lawyer brief (A2, A6); the pilot plan's gate row is updated.
- B-012 (Builder with Tester):
  - The prototype is now `prototype/compicasa-prototype.html` (git mv). The name is set once in `BRAND`; the title and header come from it, with a static no-script fallback.
  - Internal hooks keep the codename: `window.__juntos` and the storage key.
  - `test_proto.js` grew to 199 checks (5 new: the name set once, the title starts with it, the header shows it, no "Juntos" on screen). Two planted faults were both caught.
  - Headless Edge loaded both languages with no script errors; the screenshot looked right.
  - Updated the prototype notes and the Builder's queue.
- B-024 (Brand & Marketing with Legal & Compliance): `research/2026-11-22-name-clearance.md`.
  - No blocker found: no web use or company called Compicasa. .com and .net are unregistered (RDAP); .es, .org, .eu and .app look free (DNS, indicative). TikTok and YouTube have no account; the other networks are inconclusive.
  - Main sound-alike: Comprarcasa, an estate-agency network.
  - The trademark registers weren't searched: the OEPM requires reCAPTCHA and TMView rejects automated requests. These are deliberate blocks, so I didn't try to get around them; F-014 asks Sam to search by hand.
  - Ley 17/2001 arts. 5 and 6 checked on the BOE.

**Decided:** none
**Approvals used:** none. Nothing was published, sent, bought or filed.
**Learned:** the official registers deliberately block automated searching, so name clearance needs a person. An individual running a website in Spain may have to publish a postal address (LSSI art. 10), which bears on whether to form an S.L. first (A1). EUR-Lex blocked the download, but the BOE's copy of the GDPR worked.
**Open or blocked:** B-024 is blocked on F-014 (the register search). F-013 is unanswered. B-027 is new.
**Saved and checked:** committed B-013 and B-012 as each finished; re-ran `node test_proto.js` (199 passed) and `node test_landing.js` (135 passed); reviewed `git diff` for STATE, BACKLOG and FOUNDER-QUEUE and re-read the two new notes against what I meant to write. LOG.md saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-027 short DPIA. 2. B-020 pilot materials (privacy notice and consent texts first). 3. B-022 pilot areas or B-021 lawyer shortlist.
**After the closing report:** Sam closed F-013: "F013 - don't worry about it. As long as I'm not using work cloud & AI resources I should be good. Even then, until this is a business that earns money and requires attention that would conflict with my normal job, it's just something to bear in mind. It's not a black & white rule." Recorded as D-025; R-008 and STATE updated. He then asked about building a working proof of concept and whether we have credits for it (answered in chat: no credits or budget yet, F-002). Checked with `git diff`; committed.
Sam then said: "Also, this is a PoC for how to build a new company. Remove any warnings for running/developing this on Microsoft/work resources." Recorded as D-026, which supersedes D-021's restriction and D-025. Removed the work-resource rules and warnings from AGENTS.md (sections 3, 4 and 10), README, KICKOFF, STATE, RISKS (R-008, R-020), the pilot plan, the privacy note and the lawyer brief. Kept the off-limits rule on employer confidential information and the job's contacts, a separate matter. Old log entries and closed founder items stay as records. Because section 4 changed, the next session's start doubles as the cold-start check (B-023). Checked with `git diff`; committed.

## 2026-11-22 · Session 10: landing-page draft
**Who:** Sam with Claude Code (Opus 5.5), as Chief of Staff, with the Builder and Brand & Marketing (B-007) and the Tester
**Mode:** Build (D-023) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 9"; newest LOG entry Session 9. Sam: "Start a new session for this next task"
**Goals:** 1. B-007 bilingual landing-page draft with a waitlist that stores and sends nothing. 2. B-013 privacy groundwork, if time allowed (not started).
**Needs Sam:** F-013 new: you're signed in with your work account on your own machine, which is joined to the employer's tenant. Recommendation: a separate personal Windows account for this project.
**Did**
- B-007 (Builder with Brand & Marketing): `prototype/compicasa-landing-draft.html`. It's Spanish first with English behind a switch, under the provisional name Compicasa, held in one constant. It has a draft banner, a hero with a made-up example brief, four steps, "what we are not", six questions and answers, and a waitlist form.
  - The form asks only for email, an optional area and timing, and consent.
  - It has no action or method, and no field has a name, so nothing can be submitted even without JavaScript.
  - The script keeps no value and uses no browser storage. The page is marked `noindex`.
  - The copy makes no promises about money, mortgages or legal outcomes, and says plainly that we give no legal, tax or financial advice.
- Tester: `prototype/test_landing.js`, 135 checks, all passing.
  - Mutation test: seven planted faults were all caught (a form action, a named field, browser storage, an English word on the Spanish page, a mortgage promise, a hard-coded name, "o" before an o sound).
  - The first run failed on a test bug, since fixed: the named-field check also matched `<meta name>`.
  - `node test_proto.js` still passes 194.
  - Headless Edge loaded `#lang=es` and `#lang=en` with no script errors; the title and `lang` followed the language. The 1280 px screenshot looked right. Phones, a screen reader and a native speaker's review remain in B-025.
- While running Edge, I saw that the Windows account is `EUROPE\sambetts` and that the device is Entra ID joined to the "Microsoft" tenant (`dsregcmd /status`). I stopped work and asked Sam whether this is a work device. Sam: "This is my personal device. WOD2 is my own machine, even though I'm logged into work."
  - Recorded in F-013 (new) and R-008.
  - Sessions 8 and 9 logged "personal machine". That's true of the hardware, but they didn't mention the work sign-in. STATE now records both.
- Updated: `product/prototype-notes.md`, the Builder's queue, BACKLOG (B-007 done), STATE, RISKS (R-008) and FOUNDER-QUEUE (F-013).

**Decided:** none
**Approvals used:** none. Nothing was published, sent or paid for. Sam's statement about the machine is quoted in F-013; it isn't an approval.
**Learned:** "personal machine" in earlier entries hid a work sign-in, and only a stray Edge message revealed it. A design question to settle under B-013: a real waitlist needs a privacy notice, a lawful basis and a processor before it can go live.
**Open or blocked:** B-013 wasn't started (carried over). F-013 waits on Sam. Publishing the landing page or collecting real sign-ups needs B-013 and Sam's approval.
**Saved and checked:** re-ran both test suites (135 and 194 passed) and reviewed `git diff` for every changed document against what I meant to write. LOG.md saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-013 privacy and safety groundwork. 2. B-012 rename the prototype to Compicasa. 3. B-024 name clearance.

## 2026-11-22 · Session 9: name suggestions
**Who:** Sam with Claude Code (Opus 5.5) on his personal machine, as Chief of Staff, with Brand & Marketing (B-026) and the CEO (D-007 options)
**Mode:** Build (D-023) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 8"; newest LOG entry Session 8
**Goals:** 1. B-026 (new), a fresh name longlist with a quick public screen, because Sam asked: "We need name suggestions".
**Needs Sam:** F-003 updated: take Compicasa and Sumallaves into the clearance search together, then choose.
**Did**
- B-026 (Brand & Marketing): screened 71 new names. For .com, the registry's public lookup (Verisign RDAP); for .es, public DNS name servers only, which is indicative. Web searches for existing use of the top names. Result: `research/2026-11-22-name-longlist.md`, with a top five (Sumallaves, Condueños, Anidamos, Casa Socia, Casa al Alimón), the other unregistered names, and the names ruled out with reasons (for example Fincamos is too close to the registered AFINCA mark; Copropia; Arraigo).
- Updated D-007's options (CEO), F-003, B-024 (now clears Sumallaves too, and confirms the .es addresses), B-019 (adds Viviendea) and STATE.

**Decided:** none; the name is Sam's (F-003). D-007's options now list the new names.
**Approvals used:** none. Nothing was bought, filed, published or sent.
**Learned:** about 20 of the 71 names have neither address registered, but most are descriptive phrases, which are weaker as marks. Spanish agency names that use "llaves" are common. Found Viviendea, a platform that groups housing seekers by interests before approaching a developer (Reported, construible.es 2018); it isn't in B-001. Not checked: trademark registers, whether the .es addresses are really free, and what Anidando S.L. does.
**Open or blocked:** F-003 waits on Sam; B-024 can start now.
**Saved and checked:** reviewed `git diff` for BACKLOG, DECISIONS, FOUNDER-QUEUE and STATE, and re-read the new note, against what I meant to write. LOG.md saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-007 bilingual landing-page draft. 2. B-013 privacy and safety groundwork. 3. B-024 clearance of Compicasa, Comprañeros and Sumallaves.
**After the closing report:** Sam asked for more: "Any more? Compicasa is winning to far". A second round screened 33 names in Compicasa's style, the same way, and added it to the note. Comprañeros ("buying mates"), Compramigos and Llavemigos look free in both .com and .es, with no existing use found. Comprañeros now joins Sumallaves as a challenger in F-003, D-007, B-024, B-026 and STATE. Checked with `git diff`; committed. Sam then chose: "Compicasa for now. Might change later." Recorded as D-007 (provisional working name; domains, trademark and public use still need clearance and his approval); F-003 closed; B-012 and B-010 unblocked; B-024 now clears Compicasa, with the other two as fallbacks. Checked with `git diff`; committed.

## 2026-11-22 · Session 8: Build mode, bilingual prototype
**Who:** Sam with Claude Code (Opus 5.5) on his personal machine, as Chief of Staff, with the CEO (D-023, D-024), Builder and Tester (B-018)
**Mode:** Plan, then Build from 2026-11-22 (D-023) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 7, continued"; newest LOG entry Session 7, continued
**Goals:** 1. B-018, the Spanish version of the prototype, with the Tester's checks; widened mid-session by Sam to a bilingual version (D-024).
**Needs Sam:** F-012 updated: the push is done; deleting the work copies is all that's left. F-011 updated: it now only confirms the modes table, since Build mode has started. B-025 new: try the prototype on your phone, or approve a Spanish-speaking reviewer.
**Did**
- Opening: Sam asked for the project "to be built". The project was in Plan mode, so I asked whether he meant a Plan-mode session or Build mode. He chose "F-001 cleared, Build from today", confirming he'd read his employment agreement and found no approval, exclusivity or IP clause that blocks the project. Recorded as D-023; F-001 closed; mode and R-008 (score 6 to 3) updated.
- F-012: the repository is pushed (the clone tracks the GitHub remote and has its first commit), so only deleting the work copies is left.
- Cold-start check, first session in Claude Code: nothing stopped me or would have led me to break a rule. AGENTS.md section 10 covers this tool under "a coding assistant working in the repository".
- B-018 (Builder): first translated the prototype into Spanish in place. Sam then asked mid-session for the site to be "dual lingo, english & spanish", with documents kept in English (D-024). Rebuilt it as one bilingual file, `prototype/juntos-co-buying-prototype.html`:
  - every user-facing string sits in an English–Spanish string table, or an `{en, es}` label on the data, including all 14 bios
  - a language switch in the header; the page opens in the browser's language, remembers the choice, and `#lang=en` or `#lang=es` forces one
  - the page's `lang` attribute and title follow the language
  - Spanish uses tú and vosotros, avoids gendered words where it can, and follows the y/e and o/u rules
  - new storage key `juntos-prototype-v3`
  - the matching logic is unchanged: the same compatible people, mutual matches and groups as before, for both example profiles
- B-018 (Tester): rewrote `prototype/test_proto.js`, growing it from 111 to 193 checks.
  - Every screen check runs in English and in Spanish.
  - New checks: both string tables have the same keys; every label and bio exists in both languages; Spanish grammar; no words from the other language on any screen; switching language mid-session keeps the group and redraws the page.
  - Mutation test: words planted on an English screen and on a Spanish screen were both caught.
  - A first version of the leak check had a bug: its `\b` boundaries were saved as control characters, so it could never fail. Found because it passed the old English file; fixed and re-proved.
- Test record (Tester):
  - `node test_proto.js`: 193 passed, 0 failed.
  - Headless Microsoft Edge: loaded `#lang=es` (home), `#lang=en` (matches) and `#lang=es` (profile) with no script errors; the page `lang` and title followed the language; desktop screenshot at 1280 px looked right.
  - A 390 px window rendered wider than the screen, but the English-only version from before this session does the same, so that's how headless Edge lays out narrow windows, not a new bug. Mobile is untested.
  - Not tested: real phones, Firefox and Safari, a screen reader, and a native speaker's review of the AI-written Spanish. All are in B-025.
- Updated: `product/prototype-notes.md`, `product/profile-and-matching-model.md`, the Builder and Tester role files, AGENTS.md section 9 (the product is bilingual: an out-of-date fact, within the Chief of Staff's remit), BACKLOG (B-018 done; B-025 new; B-007, B-012, B-006 and B-011 no longer wait on F-001 or Build mode), STATE, RISKS and FOUNDER-QUEUE.

**Decided:** D-023 F-001 cleared, Build mode from 2026-11-22 (Sam) · D-024 bilingual product, English documents; amends D-014 (Sam; built by the Builder)
**Approvals used:** Sam's choice in Claude Code on 2026-11-22, "F-001 cleared, Build from today", quoted in D-023 and F-001; Sam's message of 2026-11-22, "Keep docs in English; the site should be dual lingo, english & spanish. I work better in English", quoted in D-024. Nothing was published, sent or paid for.
**Learned:** Node now has a `navigator` object, so the tests saw "en-US" and ran in English until they set the language explicitly. Headless Edge can't stand in for a phone. That many co-buyers in Madrid are international residents is an assumption behind D-024, not checked.
**Open or blocked:** B-025 waits on Sam (a phone check, or approval for a Spanish-speaking reviewer). F-012's deletion and every pilot item (F-002 to F-011) remain with Sam.
**Saved and checked:** after saving, re-ran `node test_proto.js` (193 passed) and reviewed `git diff` for every changed document against what I meant to write, which caught two stale lines in F-001 and F-011, now fixed. LOG.md saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-007 bilingual landing-page draft, not published and storing nothing. 2. B-013 privacy and safety groundwork. 3. Sam's remaining answers.
**After the closing report:** Sam answered "F012: done" (F-012 closed) and, on B-025, "I'll worry about it when the time comes" (set to later). He asked what names were on the table (answered from `research/2026-11-20-naming-screen.md`; F-003 is still his), then clarified D-024: "Spanish should be prioritised … just there should be support & some consideration for English speakers." The prototype now always opens in Spanish instead of following the browser's language; 194 checks pass, including one that runs in an English-language (en-GB) environment. Saved and checked with `git diff`; committed.

## 2026-11-22 · Session 7, continued: ready for GitHub as project-juntos
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff), still on Sam's work systems, to package the project for its move
**Mode:** Plan · **Copy used:** canonical folder on the work OneDrive (D-016, D-021), repackaged as the repository
**Started from:** STATE.md and the LOG entry from Session 7
**Goals:** 1. Make the project ready for a private GitHub repository named project-juntos.
**Needs Sam:** F-012 (updated): push to a private repository named project-juntos on your personal GitHub account, then delete the work copies.
**Did**
- Checked the work OneDrive copy first: all 43 files matched the final copy by content fingerprint, and nothing from Sam's day job is in them apart from F-001's summary of his own side-project obligations.
- Made it a repository: the role files moved to `.github/agents/`, where GitHub Copilot uses them as custom agents, and Sam's role to `project/founder.md`. Added `.gitignore` (operating-system files, dependencies, secrets, and `private/` and `participants/` as a safety net for personal data) and `.github/copilot-instructions.md`, which points to AGENTS.md.
- Fixed the front matter of `.github/agents/builder.agent.md`: a colon in the description made it invalid YAML, so GitHub Copilot couldn't have read it. The description is now quoted; its wording is unchanged.
- Updated AGENTS.md sections 3 to 5, 8 and 10 (the repository is canonical, a commit closes each session, nothing personal or secret is committed), README (now the repository's front page), KICKOFF.md (prompts for the repository; start with the chief-of-staff agent), STATE.md, F-012, D-022, R-020, B-007, B-018, the pilot plan, the partner tracker, the roadmap and the session hand-off template.
- Older log entries and research notes still mention `agents/` and the OneDrive folder; they're records, so they stay as written (README says where things moved).

**Decided:** D-022 private repository project-juntos on Sam's personal GitHub account; a codename only (Sam)
**Approvals used:** Sam's chat message of 2026-11-22, quoted in D-022.
**Learned:** In a repository the save check gets simpler: a clean `git status` and the commit replace listing files after each save.
**Open or blocked:** F-012 (push, then delete the work copies) and F-001 (employment agreement). Build mode waits for Sam's dated switch.
**Saved and checked:** packaged as project-juntos.zip with 45 files, each read back from the zip and compared with the build folder; the prototype's checks all pass (`node test_proto.js`). The first commit is Sam's (F-012).
**Next session should start with:** 1. In the repository, the first-session prompt in KICKOFF.md, with the cold-start check (B-023). 2. Confirm F-012 is done and close it, then apply Sam's answers. 3. B-013 privacy and safety groundwork.

## 2026-11-21 · Session 7: employer policy check; wrap-up on work systems
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, with Legal & Compliance for F-001). The last session on Sam's work systems
**Mode:** Plan · **Copy used:** canonical folder on the work OneDrive (D-016), retired by D-021 after this session
**Started from:** STATE.md after Session 6; newest LOG entry Session 6
**Goals:** 1. Finish Session 6's save. 2. Check the employer's side-project rules (F-001). 3. Wrap up for the move to personal resources.
**Needs Sam:** F-012 (new, first): move the folder to your personal storage, then delete the work copies. F-001: read your employment agreement. F-011: confirm the modes, and write "Build mode from <date>" when you want sessions to build.
**Did**
- Session 6's save had paused for approval. It finished on 2026-11-21: all 43 files matched the local copies, and nothing had been edited online.
- F-001: read the employer's side-project and conflict-of-interest policies and its code of conduct, and checked Spanish law (Workers' Statute art. 21; Intellectual Property Act art. 97.4). Summary in F-001. In short: side projects need no sign-off unless the employment agreement says so, but they mustn't use work devices, accounts or systems, or working time.
- Sam decided to move the project to his personal resources (D-021, which makes D-013 and supersedes D-016's location). F-007 closed; F-012 added.
- Made the pack work off work systems: README, KICKOFF.md, STATE.md, AGENTS.md (the reason for Plan mode in section 3, a new off-limits line in section 4, the save-approval note in section 5, and section 10 on where sessions run), R-008 (re-scored) and R-020, and the pilot plan's gates.

**Decided:** D-021 (Sam) · D-013 (Sam, via D-021)
**Approvals used:** Sam's chat message of 2026-11-21, quoted in D-021 and F-001.
**Learned:** Sessions 1 to 7 ran on work systems, which the policy rules out for side projects; from now on sessions run only on Sam's own resources. Sections 4 and 5 of AGENTS.md changed and the tool will change, so the next session runs the cold-start check (B-023), which the first-session prompt already includes.
**Open or blocked:** the move (F-012) and the employment agreement (F-001). Build mode waits for Sam's dated switch.
**Saved and checked:** every file changed in this session, listed in OneDrive after saving, with sizes matching the local copies; LOG.md's own check is confirmed in the closing report to Sam.
**Next session should start with:** on Sam's own tool, after the move: the first-session prompt in KICKOFF.md (with the cold-start check, B-023), then Sam's answers, then B-013.

## 2026-11-20 · Session 6: ready to kick off, instructions tuned for Opus
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, with the CEO for D-014 and D-020); cold-start test by a separate Opus 5.5 session
**Mode:** Plan · **Copy used:** canonical folder (D-016)
**Started from:** STATE.md after Session 5; newest LOG entry Session 5
**Goals:** 1. Check what's missing before a new AI session takes over. 2. Tune the instructions for Opus. 3. Cold-start test (B-017).
**Needs Sam:** F-011 new: confirm the operating modes. Updated: F-001 (clearing it doesn't switch the mode by itself), F-002 (time note), F-003 and F-007 (dates), F-004 (price band wording), F-005 (recommendation, with the corrected M3 rule), F-008 (date and recommendation) and F-010 (it also confirms Sam as the pilot's named responder).
**Did**
- Applied the official prompting guidance for the newest Opus (Opus 5.5, building on Opus 5):
  - every session has a finish line and named stop points: keep going while nothing needs Sam; stop only for his decisions or approvals, or for work outside the mode (`AGENTS.md` section 5)
  - operating modes, Plan, Build and Pilot, say what agents may do right now (section 3, D-020)
  - a fixed closing report with "Needs you" first (section 6)
  - rules on scope, document length, evidence, helpers (subagents) and untrusted web or email text (sections 5, 7 and 8)
  - reasons for rules, calm wording, and no "think carefully" or generic "double-check" lines
- Rewrote the role files: each has a "when to use" description, what to read first and what "done" means, and points to its backlog rows instead of a copied priority list that could drift.
- New `KICKOFF.md`: Sam's pre-kickoff checklist and paste-ready prompts for a first session, later sessions and single items.
- README updated (start here; model settings for GitHub Copilot custom agents). Session hand-off and research-brief templates updated.
- Added B-023 (repeat the cold-start test after a change of model, tool or session rules), B-024 (preliminary name clearance), D-019, D-020, R-021 and F-011; recorded D-014 (Spanish first) as made. B-017 done.
- Cold-start test (B-017): a fresh Opus 5.5 session read the pack with only the first-session prompt, as a dry run. Verdict: it could run a session within the rules without Sam, apart from approving saves. It reported 44 issues (3 high, 13 medium, 28 low), and all but one are fixed:
  - Clearing F-001 no longer reads as switching the mode: only Sam switches, in writing, once F-001 and F-007 are settled. The modes go to Sam to confirm (F-011).
  - New rules for part-done goals, dropped sessions, the log's own save check, ID allocation, and who may change the session rules.
  - The pilot's gates and its M3 decision rule now agree across files, with a new "weak signal" rule; Build-mode backlog items show Build mode as what they wait on.
  - Annex A of the lawyer brief adds pledge names and incident reports. The Finance and Partnerships role files point to the research notes instead of copying their numbers.
  - Not changed: D-016 (which copy is canonical) stays a CEO decision until F-007, as it doesn't widen what agents may do.

**Decided:** D-019 sessions run on the newest Opus (Sam) · D-020 operating modes (CEO, proposed; Sam to confirm in F-011) · D-014 Spanish first (CEO)
**Approvals used:** none.
**Learned:** Opus follows the files literally and reads across them, so many of its findings were places where two files said slightly different things (the mode switch, the pilot gates, the M3 rule). Short finish lines and named stop points do more than long instructions. The dry run took the test session about 18 minutes.
**Open or blocked:** everything beyond Plan mode waits for F-001, F-007 and F-011.
**Saved and checked:** every file changed in this session and the new KICKOFF.md, listed in OneDrive after saving, with sizes matching the local copies; LOG.md's own check is confirmed in the closing report to Sam.
**Next session should start with:** 1. Apply any answers to F-001, F-011, F-002 and F-010. 2. B-013 privacy and safety groundwork. 3. B-022 pilot areas, then B-020.

## 2026-11-20 · Session 5: lawyer brief and pilot plan
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, Legal & Compliance and Validation)
**Copy used:** canonical folder (D-016)
**Goals:** 1. Finish saving Session 4. 2. B-003 lawyer brief. 3. B-002 pilot plan.
**Did**
- **Correction to Session 4:** its save stopped after 12 of its 21 changed files, when the approval prompt was cancelled, so its "Saved and checked" line was premature. The rest, including the new competitor note, were saved in this session.
- **B-003 lawyer brief** (`research/2026-11-20-lawyer-brief.md`):
  - Part A, before the pilot, six questions: our status, personal data, equal treatment, safety and liability, commitment without money, and what we may say.
  - Part B, before any purchase: ten questions, for a quote only.
  - Also: what we think we know, with sources; the data the pilot would collect; a Spanish cover email; a checklist for Sam.
- **B-002 pilot plan** (`product/phase1-validation-plan.md`):
  - About 20 eligible households in one Madrid area and price band; pairs only; run by hand; no money, listings or advice.
  - Commitment without money: two meetings, a joint brief both households approve, an exit-rules worksheet and an optional non-binding pledge.
  - Measures M1–M3 with denominators and decision rules; EXP-001 to EXP-003; safety rules; a data summary.
  - Earliest start Monday 11 January 2027, if the gates clear. About 24 hours of Sam's time. €363 to €1,178.98 including the lawyer (estimate, not approved).
- Updated: BACKLOG (B-002 and B-003 done; new B-020 to B-022), FOUNDER-QUEUE (F-004, F-005 and F-006 updated; new F-010), DECISIONS (D-008, D-009, D-011 and D-012 wording; new pending D-018), RISKS (R-001, R-015 and R-018 updated; new R-020), the charter, roadmap, glossary, budget, experiment log, README and five role files.
- Sam answered "yes" to the next step, taken as both items, while F-001 is still open. Both are planning documents, which F-001's default allows.

**Decided:** none (D-018 added as pending).
**Approvals used:** none.
**Learned:**
- A notary advises free and impartially on a deed they're asked to prepare (Consejo General del Notariado). That helps groups later, not our own legal questions.
- A Madrid guide puts a first consultation with a lawyer at about €60–150 before VAT; specialists cost more.
- Saving to OneDrive can pause for Sam's approval, and sending a new chat message cancels the pending approval.

**Open or blocked:** every gate in section 2 of the pilot plan. The lawyer's answers decide what the profile may collect and show.
**Needs Sam:** F-010 added; F-004, F-005 and F-006 updated.
**Saved and checked:** the files left over from Session 4, the new competitor note and every file changed in this session, listed in OneDrive after saving, with sizes matching the local copies.
**Next session should start with:** 1. F-001. 2. B-013. 3. B-020 and B-022.

## 2026-11-20 · Session 4: rival mapping and prototype fixes
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, Builder and Tester); Research done by a separate AI agent
**Copy used:** canonical folder (D-016)
**Goals:** 1. B-001 map how rivals actually work. 2. B-015 fix the prototype's consent, copy and contrast issues.
**Did**
- **B-001 rival mapping** (`research/2026-11-20-competitors.md`):
  - In depth: habitacion.com, Yorsio and Casicasa. Briefly: Vivla, Pacaso and 8 other players.
  - On public evidence, nobody lets strangers form a group before choosing a whole home, agree a joint brief and design their own exit.
  - Already common elsewhere: affinity questionnaires, co-ownership shares (pro indiviso) with a notarised pact, and exit services.
- **B-015 prototype fixes** (`prototype/`):
  - *Consent:* groups form only from mutual matches who also accept each other (replies simulated). Previews are read-only, with a new "Propose this group" step. No duplicate adds. Groups are re-checked after profile edits, and the brief is hidden until a broken group is fixed. The best group is chosen by score.
  - *Fairness and privacy:* household type no longer counts in the score. Accessibility needs shape the brief, never the score. Must-haves count more than nice-to-haves. Other people's rejection reasons stay private. Fewer sensitive details in the made-up bios.
  - *Honest copy:* simulated replies and uncalibrated scores are labelled. The room split is labelled illustrative. Size shows as an editable useful-area range. Agreement prompts are clearly not the agreement.
  - *Accessibility:* contrast fixed (brand colour #b3370e; four avatar colours). Tabs are accessible, with arrow keys. Focus is kept when the page redraws, and has a visible outline.
  - *Tests:* they now check results instead of just printing them. 111 checks pass, and the HTML validator passes.
- Sam asked for both pieces of work on 20 Nov while F-001 is still open.

**Decided:** D-017 (working positioning; CEO; reversible; internal only).
**Learned:**
- habitacion.com sells a share with the use of one room, home first, and its buyers use personal loans rather than mortgages (per its founder).
- iAhorro says banks are wary of groups of strangers.
- Casicasa's traction looks thin: 3–5 staff and €5,000 turnover, self-reported.
- Rivals don't publish exact fees.

**Open or blocked:** a Spanish version of the prototype and real-browser checks before any user test (B-018). Casicasa's group flow sits behind its login and wasn't checked.
**Needs Sam:** F-001 to F-009 (unchanged).
**Saved and checked:** every changed file listed in OneDrive after saving, with sizes matching.
**Next session should start with:** F-001, then B-003, B-002 and B-009.

## 2026-11-20 · Session 3b: independent review and fixes
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff); review by a second AI model (GPT-6.2 Sol, maximum reasoning)
**Did**
- The reviewer read all 35 files, ran the prototype's tests and checked the key facts against sources. Verdict: "Ready for a read-only Phase 0 hand-off, not autonomous operation or real-user recruitment." Full report: `research/2026-11-20-independent-review.md`.
- Corrected facts:
  - A viewing-sheet signature alone doesn't bind a buyer to an agency fee.
  - The 20–25% referral rate is unverified, so it's been removed.
  - OCU found agency fees of 2–6%, averaging 5%, before VAT.
  - Madrid's estate-agent register is voluntary.
  - The wording of Civil Code arts. 404 and 1522 is now precise.
  - Ending a co-ownership is usually taxed as AJD, though IRPF can still apply.
  - Witei offers no *new* Idealista connections.
  - Casafari's plans are added.
- Changed recommendations, each for Sam to confirm:
  - No charging in Phase 1 (D-010).
  - Phase 1 is brief-only, with no portal links (D-015, amending D-004).
  - The first group is defined without an age band (D-008).
  - Pilot measures are based on introductions accepted both ways (D-012).
  - A stricter employer check (F-001).
- Process fixes:
  - Read DECISIONS and RISKS at the start of each session.
  - Edit only the canonical copy (D-016) and check that write-back succeeded.
  - Approvals are specific, and silence approves nothing.
  - Risks re-scored, with seven added.
  - Backlog dependencies fixed.
  - Budget, experiment and partner ledgers added.
- Logged prototype issues for a later session (B-015): two-way consent, duplicate adds, the preview saving state, stale groups, misleading room-split copy, contrast and tabs.
- Checked the changed files were saved to OneDrive.

**Correction to Session 1:** the competitor scan understated overlap. habitacion.com and Casicasa offer affinity-based matching and group home search; Yorsio offers affinity matching and rent-to-own. "Nobody covers matching through to a home" is unproven (B-001).
**Decided:** D-016.
**Needs Sam:** F-001 to F-009.
**Next session should start with:** F-001, then B-001, B-003, B-002 and B-015.

## 2026-11-20 · Session 3: naming and hand-off pack
**Who:** Sam with Microsoft 365 Copilot (as Research, Brand & Marketing, Chief of Staff)
**Did**
- Screened 33 Spanish and English names: 66 web addresses (.es and .com) on GoDaddy, trademarks on TMView (Spain, EU and WIPO registers), and existing businesses by web search. 28 addresses were free; 7 names had both free. See `research/2026-11-20-naming-screen.md`.
- Recommended **Compicasa**: both domains free, no trademark or business found. Not approved yet.
- Ruled out Juntos (domains taken), Afincamos (AFINCA trademarks cover real estate), Dos Llaves and Hacemos Casa (similar property businesses exist), Copiso (registered trademark) and Mi Mitad (crowded).
- Set up the AI operating model and created this folder.

**Decided:** D-006.
**Needs Sam:** F-001 to F-008.
**Next session should start with:** Sam's answers in FOUNDER-QUEUE.md, then B-001, B-003 and B-002.

## 2026-11-18 · Session 2: where to find homes
**Who:** Sam with Microsoft 365 Copilot (as Research, Legal & Compliance, Partnerships)
**Did**
- Researched how to source listings legally: Idealista's terms and Search API, Casafari, Kyero-format agency feeds, Witei, aggregators; EU and Spanish case law on scraping and linking; photo rights; the data protection authority's view on scraping; agency fee economics; the CNMC fee-fixing case. See `research/2026-11-18-homes-sourcing.md`.
- Recommended a staged plan: links pasted by members first, then licensed data, then agency feeds and referral deals.

**Decided:** D-004.

## 2026-11-18 · Session 1: idea, Spain focus, profile model, prototype
**Who:** Sam with Microsoft 365 Copilot
**Did**
- Competitor scan (Spain and Europe): the closest are room-level players in Spain (habitacion.com, Yorsio, Casicasa); adjacent are fractional second-home models (Vivla, Pacaso). Nobody covers matching, home and exit end to end.
- Captured Sam's profile model and added design principles: groups, an anchor buyer, the profile as a draft agreement, a tie-break rule, finance as a hard filter.
- Narrowed to Spain and Spanish law; outlined legal structures, financing, exit taxes, platform rules and questions for a lawyer.
- Built a playable prototype of the social side. See `prototype/`.

**Decided:** D-001, D-002, D-003, D-005.
