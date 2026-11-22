# Current state

_Last updated 2026-11-22 after Session 14 (GitHub Copilot CLI, Opus 5.5): sign-up error investigated; External ID works, the live sign-up tip corrected. By the Chief of Staff. Rewrite this file at the end of every session._

## Operating mode: Build, from 2026-11-22
Sam cleared F-001 and switched to Build mode on 2026-11-22 (D-023). Agents may build prototypes, code and unpublished drafts in this repository, using made-up data only. Nothing public, paid or sent, no one contacted and no real personal data collected without Sam (AGENTS.md sections 3 and 4). **One scoped exception (D-027):** the PoC runs in Sam's Azure subscription as if in production, closed by an invite code, with made-up people; the AI administers it through the Azure CLI. Real users, a domain, app stores or promoting it still need Sam. Pilot mode needs every gate in section 2 of `product/phase1-validation-plan.md` and Sam's dated go-ahead.

## Session in progress
None. At the start of a session, list its goals here as a checklist; clear it at the end.

## In short
The idea: help compatible strangers buy one home together in Spain, starting with the social side (matching plus a joint "what our home needs" brief). On public evidence, no rival lets strangers form a group before choosing a whole home, but affinity questionnaires, co-ownership shares with a notarised pact, and exit services already exist, so the group-first idea is what we must test. A working proof of concept now runs in Azure on free tiers (closed, made-up people, €0 a month expected: D-030); nothing else is public or bought. The project is the private GitHub repository project-juntos (D-022), and is also a proof of concept for building a new company with AI (D-026). Sam's plan (2026-11-21): build a small product and see if it gains interest. The product is Spanish first, with English supported; project documents stay in English (D-024). The pilot can't run until every gate in section 2 of its plan is met.

## Phase
**Phase 0: explore and set up**, now in Build mode. The gate to Phase 1 is every gate in section 2 of `product/phase1-validation-plan.md`, then Sam's dated go-ahead (`project/roadmap.md`).

## AI set-up
Sessions run on the newest Opus where Sam's tool offers it (D-019), as the Chief of Staff, started with the prompts in `KICKOFF.md`. Sessions 8 to 12 ran in Claude Code in the repository on Sam's machine; any machine, account or tool Sam chooses is fine (D-026); GitHub Copilot works too, using the custom agents in `.github/agents/`. Canonical copy: this repository, private on Sam's personal GitHub account (D-022). Sessions commit; Sam pushes. Don't edit other copies. Azure: the Azure CLI on Sam's machine is signed in to the PoC's subscription (81209232-…, "Visual Studio Enterprise - New").

## What exists
| Asset | File | Status |
|---|---|---|
| **Working PoC (Compicasa)** | `app/` (read `app/README.md` first) | **Live (free tiers):** https://app-compicasa-prod-25yr5.azurewebsites.net, closed by an invite code (in the web app's settings, not in the repository). ASP.NET Core on .NET 10 with the v0.3 matching rules ported to C#, scoring exactly like the prototype; accounts; likes and mutual matches (made-up people reply by simulation, real ones themselves); groups with invitations; the home brief. A PWA for phones and desktops, Spanish first with English. 14 hand-written plus 30 generated made-up people, the 30 replaced each day after 04:00 Madrid time (D-029). **Since Session 13:** email sign-up through Entra External ID (D-033) with the invite code, an 18+ box and the draft terms checked first and stored; an eight-step onboarding wizard (matches stay locked, and you stay invisible, until it's done); draft terms and privacy pages; EF Core migrations with a one-off baseline for the live database (D-034). 30 .NET tests and 32 browser checks pass locally; 14 browser checks pass on the live site, up to the hand-over to External ID (`compicasa.onmicrosoft.com`, tenant c89b050e-…, in `rg-compicasa-identity`) |
| Azure set-up | `app/infra/` | `rg-compicasa-prod` on free tiers (D-030), moved out of Spain (D-031): App Service F1 Linux in West Europe; SQL Database free offer in France Central (no single EU region offered both free tiers; kept split for now: D-032), Entra ID sign-in only, managed identity, no passwords; Application Insights capped at 0.1 GB a day; €5 monthly budget alert. Deploy with `app/infra/deploy.ps1`; tests run first. 24 browser checks pass on the free set-up |
| Instructions and kickoff guide | `AGENTS.md`, `.github/agents/`, `KICKOFF.md` | Tuned for Opus; cold-start check in Claude Code on 2026-11-22 found nothing blocking |
| Prototype (single file) | `prototype/compicasa-prototype.html` | The reference for the matching rules (the PoC's parity tests read it). 199 checks pass (`node test_proto.js`). Still to do before user tests: phones, a screen reader and a native speaker's review of the Spanish (B-025) |
| Landing-page draft (B-007) | `prototype/compicasa-landing-draft.html` | Not published; the waitlist can't send or store anything. 135 checks pass |
| Privacy and safety groundwork (B-013) | `research/2026-11-22-privacy.md` | Done; DPIA next (B-027) |
| Lawyer brief (B-003) | `research/2026-11-20-lawyer-brief.md` | Ready to send (F-006) |
| Pilot plan (B-002) | `product/phase1-validation-plan.md` | Draft v0.1 for Sam (F-010) |
| Rival flows (B-001), independent review, legal landscape, homes sourcing | `research/` | Done; the lawyer's review is pending (F-006) |
| Name screening and clearance | `research/2026-11-20-naming-screen.md`, `research/2026-11-22-name-longlist.md`, `research/2026-11-22-name-clearance.md` | Compicasa is the provisional working name (D-007). The trademark register search needs a person (F-014). No domains bought |
| Profile and matching model | `product/profile-and-matching-model.md` | v0.3, now implemented in C# (`app/src/Compicasa.Core/Matching.cs`) |
| Budget, experiments, partners | `project/budget.md`, `project/experiments.md`, `project/partners.md` | The ledger has the PoC's running cost (D-027); EXP-001 to EXP-003 planned; no partners yet |

## Decided
D-001 Spain only · D-002 groups, not just pairs · D-003 social side first · D-004 link out to listings, no scraping (amendment proposed: D-015) · D-005 prototype first · D-006 AI-run, Sam approves · D-014 Spanish first (amended by D-024) · D-019 sessions run on Opus · D-020 operating modes · D-022 private repository project-juntos · D-023 Build mode from 2026-11-22 · D-024 Spanish-first product with English supported · D-007 Compicasa as the provisional name · D-026 any machine, account or tool Sam chooses · **D-027 host the PoC in Azure as production (closed, made-up data)** · **D-028 PoC stack: .NET 10, Azure SQL Basic, App Service B1, PWA** · **D-029 made-up people refreshed daily** · **D-030 free tiers** · **D-031 out of Spain: web app in West Europe, database in France Central** · D-032 keep the two regions for now (F-015 A) · **D-033 release-ready PoC: email sign-in through Entra External ID, app emails through Azure Communication Services** · **D-034 EF Core migrations; local accounts for developers only**

## Waiting on Sam
F-002 time and budget · F-004 first customer group · F-005 pilot measures · F-006 send the lawyer brief · F-008 advisers · F-009 amend the listings decision · F-010 approve the pilot design · F-011 confirm the operating modes · F-014 trademark search, then the domains. Also: one test sign-up with his own email (B-034). Not a queue item, but his: who else, if anyone, gets the PoC's invite code (anyone but Sam is a real user: D-027).

## Next up (Build mode)
1. Close B-034 once Sam has signed up once with his own email (in a clean InPrivate window: his normal Edge profile shows "We couldn't find an account", a fresh one doesn't); fix anything he hits. If his normal profile keeps failing, find out why before real users arrive. B-045 small follow-ups.
2. B-042 switch off the household dealbreaker for real accounts (before anyone but Sam signs up).
3. B-036 messages, then B-037 notifications (ACS Email), B-038 group lifecycle, B-035 front page, B-044 demo polish: the release-readiness items, in that order.
4. Still open from before: B-027 DPIA, B-030 GitHub Actions, B-020 pilot texts (the draft terms and privacy notice in the app are placeholders for these).

## Watch out
- Production must sign people in through External ID only (D-034): `deploy.ps1 -App` refuses until `Auth__Mode` is External. Local password accounts are for developers and tests.
- External ID honours `prompt=create`: sign-up opens on its "Create account" page (Verified in a fresh Edge, Session 14). Session 13's note that it was ignored was wrong.
- Reading the External ID tenant with Graph: `az login --tenant c89b050e-… --allow-no-subscriptions` as the tenant admin, then `az account set --subscription 81209232-…` afterwards.
- Session 12's username accounts can no longer sign in (made-up data). Signing in to the new tenant changed the Azure CLI's default account; it was set back to the subscription, but check `az account show` if a command fails with a token error. The External ID sign-in record isn't removed when someone deletes their account yet (B-039).
- After changing the data model, add a migration (`dotnet ef migrations add <Name>` in `app/src/Compicasa.Api`); a test fails otherwise. Locally, delete `compicasa.db`.
- The live PoC is reachable by anyone with its address; only the invite code stops sign-ups. Don't share the code beyond Sam without his decision, and never put it in the repository.
- Free tiers (D-030): the app sleeps when idle and the first visit after a pause can take up to a minute; F1 allows 60 CPU minutes a day; the daily refresh happens when the app wakes. The web app is in West Europe and the database in France Central (D-031), kept that way for now (D-032). The €5 budget alert goes to the subscription's owners. Delete `rg-compicasa-prod` to remove everything.
- nuget.org is blocked on Sam's machine; NuGet packages come from the machine's configured feed (a mirror). Another machine will use nuget.org as normal.
- A waitlist or any website run by Sam as an individual may have to show his name and postal address (LSSI art. 10.1); a question for the lawyer before anything is promoted (B-013, A6).
- No employer confidential information or work product, and no recruiting the job's colleagues or customers (AGENTS.md section 4).
- Real participants' data must never land in this repository or in an AI session without approved terms (R-020).
- Spanish first: every user-facing string in both languages (D-024), in `i18n.js` for the PoC. The new Spanish hasn't had a native speaker's review (B-025, B-031).
- Don't use household type or money to filter real people before D-009 and the lawyer's answers (A3). The PoC's household filter is a demo with made-up people.
- Casicasa is the closest rival in concept; habitacion.com is the benchmark at scale. Lending is the other big unknown.
