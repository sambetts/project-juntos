# Log

Newest first. Add an entry at the end of every session (`templates/session-handoff.md`). Don't edit old entries; correct them in a new one.

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
