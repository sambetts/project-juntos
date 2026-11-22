# Current state

_Last updated 2026-11-22 after Session 11 (Claude Code, Opus 5.5): privacy and safety groundwork (B-013), the prototype renamed to Compicasa (B-012), and the name's public clearance check (B-024, register search left to Sam: F-014). By the Chief of Staff. Rewrite this file at the end of every session._

## Operating mode: Build, from 2026-11-22
Sam cleared F-001 and switched to Build mode on 2026-11-22 (D-023). Agents may build prototypes, code and unpublished drafts in this repository, using made-up data only. Nothing public, paid or sent, no one contacted and no real personal data collected without Sam (AGENTS.md sections 3 and 4). Pilot mode needs every gate in section 2 of `product/phase1-validation-plan.md` and Sam's dated go-ahead.

## Session in progress
None. At the start of a session, list its goals here as a checklist; clear it at the end.

## In short
The idea: help compatible strangers buy one home together in Spain, starting with the social side (matching plus a joint "what our home needs" brief). On public evidence, no rival lets strangers form a group before choosing a whole home, but affinity questionnaires, co-ownership shares with a notarised pact, and exit services already exist, so the group-first idea is what we must test. Nothing is public, nothing has been bought and no money has been spent. The project runs on Sam's personal resources, as the private GitHub repository project-juntos (D-021, D-022). Sam's plan (2026-11-21): build a small product and see if it gains interest. The product is Spanish first, because it's for Spain, with English supported for international residents and for Sam; project documents stay in English (D-024). The pilot can't run until every gate in section 2 of its plan is met.

## Phase
**Phase 0: explore and set up**, now in Build mode. The gate to Phase 1 is every gate in section 2 of `product/phase1-validation-plan.md`, then Sam's dated go-ahead (`project/roadmap.md`).

## AI set-up
Sessions run on the newest Opus where Sam's tool offers it (D-019), as the Chief of Staff, started with the prompts in `KICKOFF.md`. Sessions 8 to 10 ran in Claude Code in the repository on Sam's own machine ("WOD2"), signed in with his work account and joined to the employer's Entra ID tenant, which Sam accepts as long as no work cloud or AI resources are used (D-025); GitHub Copilot works too, using the custom agents in `.github/agents/`. Canonical copy: this repository, private on Sam's personal GitHub account (D-022). Sessions commit; Sam pushes. Don't edit other copies.

## What exists
| Asset | File | Status |
|---|---|---|
| Instructions and kickoff guide | `AGENTS.md`, `.github/agents/`, `KICKOFF.md` | Tuned for Opus; cold-start check in Claude Code on 2026-11-22 found nothing blocking |
| Prototype (Compicasa, provisional name) | `prototype/compicasa-prototype.html` | Spanish first, with an English version behind a switch in the header (D-024); opens in Spanish. Renamed from "Juntos" on 2026-11-22 (B-012); the name is set once in `BRAND`. 199 automated checks pass in both languages (`node test_proto.js`); loads without errors in headless Edge. Still to do before user tests: phones, a screen reader and a native speaker's review of the Spanish (B-025) |
| Landing-page draft (B-007) | `prototype/compicasa-landing-draft.html` | Spanish first with English; under the provisional name Compicasa; not published; the waitlist can't send or store anything. 135 checks pass (`node test_landing.js`); loads without errors in headless Edge. Publishing it, or a real waitlist, needs B-013 and Sam |
| Privacy and safety groundwork (B-013) | `research/2026-11-22-privacy.md` | Done: data map with lawful basis and retention for nine purposes, sensitive data, DPIA decision (yes, a short one: B-027), tool requirements, breach and safety incident steps, waitlist requirements. Four new questions added to the lawyer brief (A2, A6) |
| Prototype notes | `product/prototype-notes.md` | Current: how the languages work, link hooks (`#lang=en`), tests |
| Lawyer brief (B-003) | `research/2026-11-20-lawyer-brief.md` | Ready to send now F-001 has cleared (F-006): six Part A questions, ten Part B questions (quote only), the pilot's data list and a Spanish cover email |
| Pilot plan (B-002) | `product/phase1-validation-plan.md` | Draft v0.1 for Sam (F-010): about 20 households, pairs only, run by hand, no money; measures M1–M3; EXP-001 to EXP-003 |
| Rival flows (B-001) | `research/2026-11-20-competitors.md` | Done, sourced: habitacion.com, Yorsio and Casicasa in depth; Vivla, Pacaso and 8 others briefly |
| Independent review | `research/2026-11-20-independent-review.md` | Fact checks with links; fixes applied or in the backlog |
| Spanish legal landscape | `research/2026-11-18-spain-legal-landscape.md` | Corrected 2026-11-20; the lawyer's review is pending (F-006) |
| Where to find homes | `research/2026-11-18-homes-sourcing.md` | Corrected 2026-11-20; sources mostly named, not linked (links are in the review) |
| Name screening and clearance | `research/2026-11-20-naming-screen.md`, `research/2026-11-22-name-longlist.md`, `research/2026-11-22-name-clearance.md` | Compicasa is the provisional working name (D-007; may change), with Comprañeros and Sumallaves as fallbacks. Public checks found no blocker; the trademark register search needs a person (F-014). No domains bought |
| Profile and matching model | `product/profile-and-matching-model.md` | v0.3 |
| Budget, experiments, partners | `project/budget.md`, `project/experiments.md`, `project/partners.md` | First pilot estimate in the budget; EXP-001 to EXP-003 planned; no partners yet |

## Decided
D-001 Spain only · D-002 groups, not just pairs · D-003 social side first · D-004 link out to listings, no scraping (amendment proposed: D-015) · D-005 prototype first · D-006 AI-run, Sam approves · D-014 Spanish first (amended by D-024) · D-016 canonical copy (now D-022) · D-017 working positioning (internal) · D-019 sessions run on Opus · D-020 operating modes · D-021 personal resources, off work systems · D-013 where the project lives · D-022 private repository project-juntos · D-023 F-001 cleared, Build mode from 2026-11-22 · D-024 Spanish-first product with English supported; English documents · D-007 Compicasa as the provisional name · D-025 own machine with work sign-in is fine; no work cloud or AI resources

## Waiting on Sam
F-002 time and budget · F-004 first customer group · F-005 pilot measures · F-006 send the lawyer brief (unblocked by F-001) · F-008 advisers · F-009 amend the listings decision · F-010 approve the pilot design · F-011 confirm the operating modes · F-014 trademark search, then the domains; B-025 (phone and native-speaker checks) is parked until user tests come closer

## Next up (Build mode)
1. B-027 short DPIA and record of processing, made-up examples only (Legal & Compliance).
2. B-020 Spanish pilot materials with made-up data only, starting with the privacy notice and consent texts from `research/2026-11-22-privacy.md` (Product with Legal & Compliance).
3. B-022 pilot area shortlist for F-004 (Research with Validation), or B-021 lawyer shortlist for F-006 (Partnerships).
4. Sam: F-014 (ten-minute register search) and F-011, then the pilot items F-010, F-004, F-005 and F-006.

## Watch out
- A waitlist or any website run by Sam as an individual may have to show his name and postal address (LSSI art. 10.1); that's a question for the lawyer before anything is published (B-013, A6).
- Sessions run on Sam's own machine under his work sign-in, which he's fine with (D-025). The line: no work cloud or AI resources (the employer's OneDrive, tenant storage, Copilot and similar) for this project.
- F-012 closed 2026-11-22: the work copies are deleted.
- Nothing for this project on Sam's work devices, accounts, systems or tools, or in his working time; no employer confidential information or work; no approaching colleagues or his employer's customers through work (F-001, D-021).
- Build mode allows building, not publishing: no hosting, deploys, domains, sign-ups, analytics or forms that send data without Sam's dated approval.
- Real participants' data must never land in this repository (git history keeps it), on work systems or in an AI session without approved terms (R-020). Only pseudonymised IDs and totals belong here.
- Spanish first: Spanish is the default and the reference wording, and English must also exist for every user-facing string (D-024); the tests fail if one is missing or a screen mixes languages. The Spanish hasn't had a native speaker's review yet (B-025).
- The pilot's commitment step involves no money. A refundable deposit needs the lawyer's clearance first (D-010; lawyer brief A5).
- Don't use household type or money to filter real people before D-009 and the lawyer's answers (A3). The prototype's household filter is demo-only, with made-up people.
- Casicasa is the closest rival in concept; habitacion.com is the benchmark at scale (250 rooms and studios sold, €1.7M raised). Watch whether its group service opens to strangers (B-019).
- Lending is the other big unknown: iAhorro says banks are wary of groups of strangers, and habitacion.com avoids mortgages altogether.
- Domain availability, checked on 2026-11-20, can change any day.
