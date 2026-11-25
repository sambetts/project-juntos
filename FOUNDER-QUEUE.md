# Founder queue: what needs Sam

Agents add requests here. Sam replies by writing **ANSWER:** under an item. The CEO records each answer in DECISIONS.md, and the Chief of Staff closes the item. Silence, or a partial answer, approves nothing. "By" dates are suggestions.

## Open

### F-044 · Push the front-end review to `main` (deploys B-083) · CLOSED 2026-11-25: pushed
**Why:** a push deploys (D-041). It brings the twelve changes you approved ("Do it all. Make it amazing", D-054): the current tab shows, Home is a tab, toasts at the bottom, a readable colour for you, two-line options, a balanced group page, a slim preview ribbon, one line instead of a row of identical 100 scores, better dark-mode plans, singular Spanish, and a front page for visitors.
**Options:** push now; or look at it locally first (`dotnet run` in `app/src/Compicasa.Api`, invite code `local-test`).
**Recommendation:** push now. Front end only: no database or API change. 99 of 99 browser checks and 118 .NET tests pass.
**Suggested date:** 2026-11-26.
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-25, in Claude Code):** "push". In reply to the Session 33 closing report asking him to approve F-044: approval to push the front-end review commits to `main` only.

### F-043 · Push the code review fixes to `main` (deploys B-082) · CLOSED 2026-11-25: pushed
**Why:** a push deploys (D-041). The fixes stop a failed sitemap from hiding about half the homes for a day, stop blocked people sending notifications, make account deletion all-or-nothing, and cut the database work behind the 10- and 30-second refreshes and the house browser (D-053).
**Options:** push now; or wait.
**Recommendation:** push now. 118 .NET tests (5 new) and the browser test (99 of 99) pass locally. After the next 05:00 crawl, the WebJob log should end `Partial = False`; `True` means a sitemap couldn't be read and nothing was marked gone.
**Suggested date:** 2026-11-26.
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-25, in Claude Code):** "push". In reply to the Session 32 closing report asking him to approve F-043: approval to push the code review fixes to `main` only.

### F-042 · Push in-app feedback to `main` (deploys B-081) · CLOSED 2026-11-24: pushed
**Why:** a push deploys (D-041). Signed-in people get *Send feedback* in the menu and footer (an idea, something's wrong, something I like; a message; an optional 1–5 rating); you read it on `#inbox` with filters by kind and the average rating. Migration `Feedback` adds two empty columns to the contact table, applied at start-up.
**Options:** push now; or wait.
**Recommendation:** push now. 113 .NET tests and the browser test (99 of 99) pass locally. You still need your account id in `Admin__AccountIds` to read `#inbox` (send it from `/api/me`).
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "push". In reply to the closing report asking him to say "push" for F-042: approval to push the in-app feedback commits to `main` only.

### F-041 · Push the home photos to `main` (deploys B-080, D-051) · CLOSED 2026-11-24: pushed
**Why:** a push deploys (D-041). Each real home's card then shows the advert's main photo, loaded from the agency's server (never copied), credited "Foto: Tecnocasa", with the sketch as fallback; the bot page and "Where homes come from" say so. Migration `HomePhotoUrl` adds one column. Homes already in the index get their photo when the crawl re-reads them, within the daily cap of 3,000, so all of them within about two days.
**Options:** push now; or wait for the lawyer's view on A7 first.
**Recommendation:** push now. 98 .NET tests and the browser test (93 of 93) pass locally; R-029 is low and one setting turns photos off.
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "push". In reply to the closing report asking him to say "push" for F-041: approval to push the home photos commits to `main` only.

### F-039 · Push the house browser to `main` (deploys B-064) · CLOSED 2026-11-24: pushed
**Why:** a push deploys (D-041). It adds the *Viviendas* tab (`#houses`): every home in the index with filters, a fit score against your own profile, saved homes, and "add to a group's shortlist"; plus one new table (`SavedHomes`, applied at start-up). It also fixes a clash in the contact inbox's page state with the Alerts tab (nobody could reach it yet).
**Recommendation:** yes. 97 .NET tests and the browser test (93 of 93) pass locally.
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "push." In reply to the closing report asking him to say "push" for F-039: approval to push the house browser commits to `main` only.

### F-040 · Which map tiles for the house browser's map (B-079) · by 2026-12-05
**Why:** a map needs tile images from somewhere, and that server sees each visitor's IP address. **Options:** A) OpenStreetMap's free tiles (light use only under its policy; attribution) · B) a commercial free tier (an account in your name) · C) no map for now.
**Recommendation:** A while traffic is small, named in the privacy notice.
**If no answer:** the map waits.

### F-038 · Push the contact form to `main` (deploys B-078) · CLOSED 2026-11-24: pushed
**Why:** a push deploys (D-041). It adds `#contact`, open to anyone, storing messages in the database for you to read at `#inbox`, and a database migration (one new table, applied at start-up). Nothing is emailed until Compicasa has an address and domain (F-014).
**Recommendation:** yes. 94 .NET tests and the browser test pass locally. Before real users the privacy notice must mention it (B-052).
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "that's fine. Make it happen." In reply to the closing report asking him to say "push" for F-038 and "may I set your account ID in `Admin__AccountIds`?": read as approval for both, the push and setting the live app setting to his own account ID only.

### F-037 · What the site shows about you as its operator (LSSI art. 10.1) · before release
**Why:** you'll run Compicasa as an individual (D-050). If a free site counts as an information society service (a question we can't settle without a lawyer), the law asks for the operator's name, address, email and NIF on the site (Verified, BOE). That would be your own.
**Options:** A) name, email, a postal address and NIF, as the law lists · B) name and a contact email only, accepting the risk if the law applies (a serious breach is fined from €30,001) · C) a paid PO box or virtual address (a cost, against D-050).
**Recommendation:** A, on the legal notice page, unless you'd rather not publish your address; then B, knowingly.
**If no answer:** the release waits for this item.

### F-036 · Push Session 28's release-checklist work to `main` (deploys B-042 and B-074) · CLOSED 2026-11-24: pushed
**Why:** a push to `main` deploys the app (D-041). It brings: household type no longer filters real people (B-042, a rule in AGENTS.md section 4), and the release funnel page `#funnel` (B-074). No database change. B-073 (the subscription move kept ready) changes only infrastructure files, which the push doesn't run.
**Also:** after the deploy, may I set `Admin__AccountIds` on the live web app to your account id, so you alone can open `#funnel`?
**Recommendation:** yes to both. 90 .NET tests and the browser test (85 of 85) pass locally.
**If no answer:** committed, not deployed.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "push". Read as approval to push `main` so GitHub Actions deploys B-042 and B-074, within D-027. Not an answer to setting `Admin__AccountIds` (asked again in the closing report).

### F-032 · Move hosting off the Visual Studio credit before anyone else signs up (R-024) · CLOSED 2026-11-24: stay for now (D-049)
**Why:** your subscription is the Visual Studio subscriber offer. Microsoft's terms: "This benefit is for development and testing only. Production workloads must be run on regular subscriptions." The credit also stops if that Visual Studio subscription ends. Fine for the closed preview; not for a public tool.
**Options:** A) a pay-as-you-go subscription in your name for the live app (about €16 a month before VAT estimated in D-028, to re-price; the credit stays for testing) · B) apply for startup credits meant for production (eligibility not checked) · C) stay as now.
**Recommendation:** A, set up before release; B in parallel if you're eligible. Opening the subscription is yours; I then redeploy from Bicep.
**If no answer:** nothing opens to the public.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "I'm happy to run in the VS sub for now, assuming we can migrate to another one later". Read as option C for now: the release may run on subscription 81209232-… (Visual Studio), knowing Microsoft's terms say development and testing only, provided a move to another subscription stays possible. Recorded as D-049. Not an approval to open to the public (F-033).

### F-033 · Approve the release checklist as the gate to real users (changes AGENTS.md section 3) · by 2026-12-05
**Why:** the free-release strategy (D-048) needs a gate. Today Pilot mode needs every gate in section 2 of the pilot plan, which assumes a hand-run pilot. Changing the modes is yours.
**Ask:** approve section 4 of `project/strategy.md` (11 items: hosting, who operates it, lawyer Part A, privacy and DPIA, DSA basics, deletion, safety, measurement, Spanish review, domain) as Pilot mode's gate; you remain the named responder.
**Recommendation:** approve. Updated 2026-11-24 for D-050: item 2 is now the operator notice (F-037) and item 3 conservative defaults instead of the lawyer.
**If no answer:** the old gate stands and nothing opens.

### F-034 · Who runs Compicasa in law: you as an individual, or a company (S.L.)? · CLOSED 2026-11-24: as an individual (D-050)
**Why:** once public, the site may have to show the operator's name, address and NIF (LSSI art. 10.1, Verified), and fines or claims fall on the operator. With an individual, that is you, personally (R-026).
**Options:** A) ask the lawyer first (question 1 in `research/2026-11-24-free-launch-rules.md`), then decide · B) individual now · C) form an S.L. now (costs not checked).
**Recommendation:** A, within the F-006 consultation.
**If no answer:** checklist item 2 stays open and nothing opens.
**ANSWER (Sam, 2026-11-24, in Claude Code):** "Creating an actual company I'm not going to do until it's absolutely necesary. Unless we're charging money I don't see the need yet and that has it's own costs. The aim is to have as close to 0 costs until the interest is proven. Lawyer too." Recorded as D-050: an individual until charging; no lawyer until interest is proven.

### F-035 · What counts as critical mass, and where we focus (replaces F-004, F-005, F-010) · Sam 2026-11-24: decide later; suggested milestones in `project/strategy.md` section 4
**Why:** D-048 says charge only at critical mass; we need the number now so we measure the right things from day one.
**Proposal** (`project/strategy.md` section 4, all Assumptions): in Madrid, buying to live in, over a quarter: 1,000 completed profiles, 60% with a mutual match in 14 days, 30 groups formed, 10 agreeing on a home, and one group-made purchase at any time. Pivot rule: after 90 days of real promotion, under 10% matched or no agreed brief means change course or stop.
**Recommendation:** approve or edit the numbers; retire F-004, F-005 and F-010, keeping the pilot plan as a fallback.
**If no answer:** we measure the funnel but declare nothing.

### F-031 · Widen the mission wording to include buying to rent out (D-047) · by 2026-11-30
**Why:** AGENTS.md section 1 says the mission is helping people buy a home to live in together. Since B-063 (your request) the app also matches people buying to rent out. Changing the rulebook's mission needs you.
**Options:** A) add "or to rent out" to section 1; B) keep the mission about homes to live in, and treat renting out as a secondary path; C) remove the rent-out path.
**Recommendation:** A, with "to live in first" kept as the pilot's focus (D-008), since the feature is built and you called it a big differentiator.
**If no answer:** the feature stays live; the mission text stays as it is.

### F-030 · Push Session 26's work to `main` (B-063, B-061, B-060) · CLOSED 2026-11-23: approved in advance
**Why:** a push to `main` deploys the app (D-041).
**ANSWER (Sam, 2026-11-23, in Claude Code, after the three items were started):** "push when everything done. Weeee, straight to prod we goooo". Read as approval to push `main` once B-063, B-061 and B-060 are merged and the .NET and browser tests pass, within D-027. Not an approval for anything else (no real users, emails or spending).

### F-029 · Push *Where homes come from* and the "preview version" wording to `main` (B-062) · CLOSED 2026-11-23: pushed
**Why:** a push to `main` deploys it (D-041). The new page and endpoint are public like the guide and `/bot`: agency names and counts only. No migration.
**Options:** A) push now; B) wait and push with the live-in/rent-out work (B-063).
**Recommendation:** A. 74 .NET tests and 78 browser checks pass locally.
**If no answer:** it stays committed; the live site keeps saying "proof of concept".
**ANSWER (Sam, 2026-11-23, in Claude Code):** "push and then work on backlog items in new sesssions". Read as approval to push `main` so GitHub Actions deploys B-062, within D-027. Not an approval for anything else.

### F-028 · Push the corrected tax rates to `main` (B-059) · CLOSED 2026-11-23: pushed, see LOG Session 24 continued
**Why:** checked against each region's own law, two rates in the cost estimate were too high: Galicia is 8% (we had 9%) and Murcia 7.75% (we had 8%). A push to `main` deploys the fix (D-041). No migration.
**Options:** A) push now; B) wait and push with the next change.
**Recommendation:** A. A one-line data fix; 73 .NET tests pass and the browser test passes, apart from an intermittent timeout on closing a person's card that has nothing to do with this change (B-061).
**If no answer:** it stays committed; the live estimate overstates tax for homes in Galicia and Murcia.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "push". Read as approval to push `main` so GitHub Actions deploys the rate fix, within D-027. Not an approval for anything else.

### F-027 · Push "all of Spain" to `main` (B-059, D-045) · CLOSED 2026-11-23: pushed, see LOG Session 24
**Why:** the app now covers every province, not only Madrid: the area picker, placing homes, the crawl and the tax in the cost estimate. A push to `main` deploys it through CI/CD (D-041). No migration.
**Options:** A) push now; B) look at it locally first.
**Recommendation:** A. 73 .NET tests and 78 browser checks pass locally. After the deploy, the next daily crawl starts adding Tecnocasa homes from all of Spain, up to 3,000 adverts a day as now, so the rest of Spain fills in over several days. Profiles you already have keep their Madrid areas.
**If no answer:** it stays committed and undeployed.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "push". Read as approval to push `main` so GitHub Actions deploys Session 24, within D-027. Not an approval for anything else.

### F-026 · Push the maximum budget to `main` (B-058, D-044) · CLOSED 2026-11-23: pushed, see LOG Session 23
**Why:** profiles now have a private maximum budget, and a group's homes are filtered by the members' budgets added up. A push to `main` deploys it through CI/CD (D-041). No migration: profiles are stored as JSON.
**Options:** A) push now; B) wait to see it locally first.
**Recommendation:** A. 58 .NET tests and 74 browser checks pass locally. On the live site, your account will have no budget until you set one (Profile, "Your budget"), so groups you're in won't filter by price until then.
**If no answer:** it stays committed and undeployed.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "push it". Read as approval to push `main` so GitHub Actions deploys Session 23, within D-027. Not an approval for anything else.

### F-025 · Push house voting to `main` (B-056, D-043) · CLOSED 2026-11-23: pushed, see LOG Session 22
**Why:** a push to `main` deploys through CI/CD (D-041) and applies the `Shortlist` migration (two new tables, nothing dropped).
**ANSWER (Sam, 2026-11-23, in Claude Code):** "fix the rest then push". Read as approval to fix B-057 and confirm the estimate's tax rate, then push `main` so GitHub Actions deploys Session 22, within D-027. Not an approval for anything else, and not an answer to F-024 (agency emails stay off).

### F-024 · Email drafts to real agencies once a group agrees on a home (B-056, D-043) · by 2026-12-07
**Why:** when a group agrees on a home, the app now drafts a viewing request and an offer email for one member to send from their own email. It does this for made-up homes only. For real homes (Tecnocasa adverts) it says the feature is off, because a member could then contact a real company through our draft, which is yours to decide (AGENTS.md section 4).
**Options:** A) keep it off until Pilot mode and the lawyer's review · B) switch it on now for you only, to try it with one agency yourself · C) switch it on for everyone
**Recommendation:** A. There are no real users yet, and an agency partner (B-011) would be the better route.
**If no answer:** it stays off for real homes.

### F-023 · Push the start-up screen to `main` (B-054) · CLOSED 2026-11-23: pushed and deployed, see LOG Session 21
**Why:** the start-up screen was committed (27e2735) but not pushed; a push to `main` deploys through CI/CD (D-041). No database change.
**Options:** A) push now · B) not yet
**Recommendation:** A.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "Yes, push it". Read as approval to push `main` (Sessions 20 and 21, including the CI/CD workflow) so GitHub Actions deploys it, within D-027. Not an approval for anything else.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "You run it". Read as approval for the AI to add one federated credential, `github-main-ids` (subject `repo:sambetts@34576795/project-juntos@1467408746:ref:refs/heads/main`), to the app registration `compicasa-github-deploy`, and to re-run deploy run 45171544394. Not an approval for anything else.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "Once you've done that, push so it deploys". Read as approval to push `main` once the header menu (B-055) is built and checked, deploying it with the start-up screen fixes. Not an approval for anything else.

### F-022 · Deploy the Home page (B-053, D-040) · CLOSED 2026-11-23: deployed, see LOG Session 20
**Why:** built and tested locally (47 .NET tests, 54 browser checks). The logo now opens a Home page: where you are (profile, people, group, home) with one next step, a profile score, and three homes for you and your best match. The "Proof of concept · made-up people" badge is gone from the header; the footer and the "Made up" tags remain. No database change.
**Options:** A) deploy with `deploy.ps1 -App` and run the live checks · B) not yet
**Recommendation:** A, today or tomorrow.
**If no answer:** stays local; the live site keeps Session 19's version.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "deploy". Read as approval to deploy Session 20 with `deploy.ps1 -App` and run the live checks, within D-027. Not an approval for anything else.

### F-021 · Deploy notifications, messages and the group lifecycle (B-036, B-037, B-038) · CLOSED 2026-11-23: deployed, see LOG Session 19
**Why:** built and tested locally (47 .NET tests, 46 browser checks, 14 two-person checks). The deploy also adds a database migration (`Social`: five new tables and four columns, nothing dropped) and fixes a live bug: opening or reloading the app at a People address (e.g. `#people-review`) stays on "Loading".
**Note:** likes now tell the other person (D-039). On the live site only you are real, so you'll see the made-up side: "♥ Liked you", scripted replies, the bell.
**Options:** A) deploy with `deploy.ps1 -App` and run the live checks · B) not yet
**Recommendation:** A, this week.
**If no answer:** stays local; the live site keeps Session 18's version.
**ANSWER (Sam, 2026-11-23, in Claude Code):** "deploy". Read as approval to deploy Session 19 with `deploy.ps1 -App` (including the `Social` migration) and run the live checks, within D-027. Not an approval to push.

### F-016 · Create the sign-in tenant for the new sign-up (D-033) · CLOSED 2026-11-22: done, see LOG Session 13
**Why:** the new email sign-up is built and tested locally (B-034), but goes live only once Microsoft Entra External ID exists. Creating it makes a new directory linked to your subscription, and Claude Code's safety check stopped me doing it, so it's yours to run or approve. Cost: free up to 50,000 monthly users.
**Ready:** `app/infra/external-id.ps1` (please read it first). It asks you to sign in to the new tenant once.
**Options:** A) run it yourself: `! pwsh app/infra/external-id.ps1`, then tell me and I'll deploy; B) tell me to run it and approve the prompt; C) not now.
**Recommendation:** A, today.
**Note:** after the deploy, Session 12's username accounts can't sign in (made-up data only).
**If no answer:** the live site keeps the old sign-up.

**ANSWER (Sam, 2026-11-22, in Claude Code):** "You can't create External ID tenants? Creating one from tenant ID 97aa2ef1-f5e5-44d7-87a2-4655816f4806 I did previously with claude for another app", then, asked whether the AI should run `app/infra/external-id.ps1` now: "yes". Option B; recorded under D-033. (97aa2ef1-… is the subscription's own directory.)
Then, asked whether to deploy the new version and check the live site: "do it all" (Sam, 2026-11-22, in Claude Code). Covers `deploy.ps1 -App` and the live checks, within D-027.
Then, for the sign-up tip fix: "deploy" (Sam, 2026-11-22, in Claude Code).
Then, after his sign-up still failed: "Fix production" (Sam, 2026-11-22, in GitHub Copilot CLI). Read as approval to deploy the corrected sign-up tip with `deploy.ps1 -App` and run the live checks, within D-027.

### F-002 · Time and money for Phase 1 (about six weeks) · by 2026-11-27
**Options:**
- A) about 2 hours a week (12 hours in total), up to €300
- B) about 5 hours a week (30 hours in total), up to €1,000
- C) your own numbers

**Note:** the pilot plan needs about 24 hours of your time over six weeks, so option A works only if a delegate runs the interviews.
**Recommendation:** B. Finance costs the plan against your choice (B-008).
**If no answer:** no spending; research only.

### F-004 · First customer group (D-008) · by 2026-12-04
**Recommendation:** start with two households searching in one Madrid area and one home price band set for the pilot, which participants opt into themselves (we don't collect their finances), planning to buy within 12 months; keep families exploratory. Research and Validation will propose two or three candidate areas (B-022) for you to pick.
**If no answer:** the pilot plan stays a draft and nothing runs.

### F-005 · Pilot success and stop measures (D-012) · by 2026-12-04
**Ask:** approve or edit the measures and decision rules in section 8 of `product/phase1-validation-plan.md`: the two in `project/charter.md`, plus M3 (pairs that commit as a group before seeing any home, with no money involved).
**Recommendation:** approve, with the decision rules as corrected on 2026-11-20: "continue" now needs M3 at two or more pairs, and a "weak signal" rule covers one to five mutual acceptances.
**If no answer:** the pilot doesn't run.

### F-006 · Send the lawyer brief and book Part A · DEFERRED 2026-11-24 by Sam until interest is proven or before charging (D-050)
**Why:** the pilot's data, matching rules and safety terms depend on the answers, and later the legal structure and revenue model too (D-009 to D-011).
**Ready:** `research/2026-11-20-lawyer-brief.md`. Its first part is for you only (a checklist); section 8 is the Spanish cover email.
**Ask:** from a personal email address, send it to two or three lawyers or firms (Partnerships can shortlist them: B-021) and compare fixed-fee quotes. Then book Part A: one hour plus a short written note. Part B is a quote only for now.
**Recommendation:** choose someone who covers property law and data protection, and approve the fee through F-002.
**If no answer:** the legal questions stay open, and nothing with real users goes ahead.

### F-008 · Co-founder or advisers? · by 2026-12-18
**Ask:** do you know a Spanish property lawyer, an estate agent or a mortgage broker who might advise? Answer only if you know someone: name two or three, in chat or here, and Partnerships drafts the outreach for your approval. We record only their organisation.
**Recommendation:** answer only if a name comes to mind; the lawyer shortlist (B-021) covers the rest.
**If no answer:** Partnerships researches candidates without contacting anyone.

### F-010 · Approve the pilot design (B-002, D-018) · by 2026-12-04
**Why:** the Phase 1 pilot plan is drafted: `product/phase1-validation-plan.md` (its first section is a summary). About 20 households, pairs only, run by hand; no money, listings or advice; about 24 hours of your time over six weeks.
**Options:** A) approve as drafted · B) approve with edits · C) not now
**Recommendation:** A. Approving the design doesn't start anything: recruiting, contacting people and collecting data still need every gate in its section 2 and a separate, dated go-ahead from you. Approving also confirms you as the pilot's named responder for safety and data incidents (reply within 24 hours), unless you name a delegate.
**If no answer:** the plan stays a draft; agents prepare materials with made-up data only (B-020).

### F-011 · Confirm the operating modes (D-020) · by 2026-11-27
**Why:** the modes decide what the AI may do on its own, before and after the employer check (AGENTS.md section 3). They were proposed on 2026-11-20 while tuning the instructions for Opus, and only you should be able to loosen them.
**Update (2026-11-22):** you've since switched to Build mode (D-023), so this now only confirms the table itself: what Build mode allows (prototypes, code and unpublished drafts in this repository; nothing public, paid or sent) and the gate for Pilot mode.
**Options:** A) confirm as written · B) edit
**Recommendation:** A. Every later mode still needs your dated switch.
**If no answer:** the modes apply as written in AGENTS.md section 3, and Build mode continues.

### F-014 · Ten-minute trademark search for Compicasa, then the domains · by 2026-12-06 · 2026-11-24: Sam will also set up an email address with the domain ("Add a contact us form. We'll need to setup an email address & domain (pending me)."); the contact form then notifies it
**Why:** the public checks found no blocker (`research/2026-11-22-name-clearance.md`): no web use, no company, and .com and .es look free. But the trademark registers (OEPM and TMView) block automated searches, so a person has to run them. The closest sound-alike is Comprarcasa, an estate-agency network.
**Ask:** run the searches in that note's "Steps" section (about ten minutes), or name a delegate. Paste the results here.
**Then:** if nothing similar shows up in classes 35, 36, 42 or 45, approve buying compicasa.es and compicasa.com (€32.98 listed on 2026-11-20; re-check the price), from a personal account.
**Recommendation:** do the search this fortnight; buy only after it's clean. File a trademark later, with the lawyer.
**If no answer:** Compicasa stays a provisional working name; nothing is bought or published.

## Closed

### F-019 · Remove made-up homes from the live site · closed 2026-11-22
**Asked in:** chat after Session 16's deploy. Sam: "So just to check; the house fake data is regened each day? If we have a proper crawler, we probably don't need fake houses". Recommendation given: a setting that turns made-up homes off (on locally and in tests, off live), delete the 90 live rows, keep the "no home fits yet" message.
**ANSWER (Sam, 2026-11-22, in Claude Code):** "Do it. First though, give me the invite code". Read as approval to build, test and deploy that change, including the app setting and deleting the made-up rows from the live database, within D-027. The invite code was given to Sam in chat only.

### F-020 · Deploy the guide and the calmer navigation (B-050, B-051) · closed 2026-11-23
**Asked in:** the Session 18 closing report ("a go to deploy this together with the Session 17 guide (`deploy.ps1 -App`)").
**ANSWER (Sam, 2026-11-23, in Claude Code):** "you deploy". Read as approval to deploy B-050 and B-051 with `deploy.ps1 -App` and run the live checks, within D-027. Not an approval to push.

### F-018 · Deploy the design review's explorers (B-049) · closed 2026-11-22
**Asked in:** the Session 16 closing report ("say 'deploy' and I'll run `deploy.ps1 -App`, then check it on the live site with the real Tecnocasa homes").
**ANSWER (Sam, 2026-11-22, in Claude Code):** "deploy". Read as approval to deploy B-049 with `deploy.ps1 -App` and run the live checks, within D-027. Not an approval to push.

### F-009 · Amend the listings decision (D-015) · closed 2026-11-22
**Why:** the review found that links pasted by members lower the scraping risk but aren't permission, and Idealista's terms make no exception for them.
**Recommendation:** accept. Phase 1 is brief-only, with no listing links; later, only licensed or authorised links.
**If no answer:** D-004 stands, but the Homes tab (B-014) stays parked.
**Closed 2026-11-22:** superseded by F-017 and D-035 (agency websites may be crawled, never portals; facts and a link only).

### F-017 · An index of homes for sale: how far may we go? (amends D-004, D-015) · closed 2026-11-22
**Why:** you asked for our own database of homes, scraped daily from agencies, matched to groups and linking to the advert. D-004 and AGENTS.md section 4 rule out scraping until you change them. Tests (`research/2026-11-22-listings-index.md`): all nine agency networks checked restrict it in their terms, and the database right (LPI art. 133.2) covers daily re-crawling.
**Options:** A) build it with made-up homes, hand-seed a few dozen real ones (facts and link only), automate per agency once it says yes · B) A plus automated crawling of agency sites now (not portals), facts and link only · C) made-up homes only
**Recommendation:** A, and add the questions to the lawyer brief.
**If no answer:** C.
**ANSWER (Sam, 2026-11-22, in Claude Code):** chose "B: crawl agency sites now": "A, plus a daily automated crawl of agency websites (never portals). It would honour robots.txt and store only facts and the link." Recorded as D-035.
Then, after the closing report offered to deploy, run a first crawl into the live database and set up the daily run (B-047): "deploy and push" (Sam, 2026-11-22, in Claude Code). Read as approval to deploy with `deploy.ps1 -App`, run the first live crawl, and push the repository.
Then, after Claude Code's safety check refused the SQL firewall rule for the crawl: "A", then "You fix it" (Sam, 2026-11-22, in Claude Code). Read as approval for the AI to add a temporary firewall rule (`crawl-session15`, this machine's IP only) for the first live crawl, and remove it afterwards.

### F-015 · One Azure region for the PoC: how? · closed 2026-11-22
**Why:** you asked for everything in one European region (D-031). On 2026-11-22 no EU region offered both free tiers on your subscription. France Central has the free database but no free web-app quota; West Europe has the free web app but accepts no new databases. For now the web app is in West Europe and the database in France Central, the next region over, at €0. It works, and all 24 browser checks pass.
**Options:**
- A) Keep it as it is: €0, two neighbouring regions.
- B) Ask Microsoft for free web-app (F1) quota in France Central, then I move the web app there: one region, €0. You file it in the Azure portal (Quotas › App Service › France Central › F1 › 1). It's free and often approved quickly, but that isn't guaranteed.
- C) One region now, not quite free: the database on SQL Basic in a region with F1 (for example Sweden Central), about €4.40 a month before VAT.

**Recommendation:** B, keeping A until it's approved.
**If no answer:** A stays.
**ANSWER (Sam, 2026-11-22, in Claude Code):** "We'll go A for now". Recorded as D-032.

### F-013 · Your work account on your own machine · closed 2026-11-22
**Why:** in Session 10 the Chief of Staff found that the Windows account running these sessions is your work account (`EUROPE\sambetts`), and that the machine is joined to the employer's Entra ID tenant (`dsregcmd /status`: AzureAdJoined YES, tenant "Microsoft"). Your employer's policy allows this project only outside work devices, accounts and systems (F-001, D-021). The device is yours: **ANSWER (Sam, 2026-11-22, in Claude Code):** "This is my personal device. WOD2 is my own machine, even though I'm logged into work." Being signed in through the employer's tenant may still give its device management (policies, backup, monitoring) reach over the project files.
**Options:** A) carry on as now · B) use a separate personal Windows account (not joined to the tenant) for this project · C) check with your employer that this is fine
**Recommendation:** B. It's quick, and it keeps the project clearly off work accounts and systems.
**If no answer:** sessions carry on here, and the risk stays open in R-008.
**ANSWER (Sam, 2026-11-22, in Claude Code):** "F013 - don't worry about it. As long as I'm not using work cloud & AI resources I should be good. Even then, until this is a business that earns money and requires attention that would conflict with my normal job, it's just something to bear in mind. It's not a black & white rule." Recorded as D-025.

### F-003 · The name (D-007) · closed 2026-11-22
**Recommendation:** keep Compicasa as the provisional favourite. Before approving or spending, run a clearance search for similar names in the services we'll actually offer (Compricasa, Compracasa, and the rival Casicasa), and a quick comprehension test with Spanish speakers. Brand & Marketing can do a first pass on the public registers now (B-024). Then buy compicasa.es and compicasa.com (€10.99 + €21.99 = €32.98 listed on 2026-11-20; prices and availability can change) and file a trademark for those services.
**Update (2026-11-22):** at your request, a fresh longlist of 71 names: `research/2026-11-22-name-longlist.md`. New recommendation: keep **Compicasa** as the favourite and take **Comprañeros** ("buying mates": it says *buying*, which fixes Compicasa's flat-share risk) and **Sumallaves** ("your keys add up") into the clearance search with it, then choose. All three looked free in .com and .es. Runners-up: Compramigos, Condueños, Anidamos. Nothing to buy yet.
**If no answer:** "working name" everywhere; nothing published.
**ANSWER (Sam, 2026-11-22, in Claude Code):** "Compicasa for now. Might change later." Recorded as D-007: Compicasa is the provisional working name. Buying the domains and filing a trademark aren't approved yet; they wait for the clearance search (B-024) and a separate request.

### F-012 · Push to the private repository, then delete the work copies · closed 2026-11-22
**Done (seen in Session 8, 2026-11-22):** the repository is pushed: the local clone tracks `github.com/<Sam's account>/project-juntos` and has its first commit ("Initial"), and Session 8 ran in it on Sam's personal machine.
**Still open:** delete the work copies: the "Co-buying startup" folder at the root of your work OneDrive, and this project's task folder in its Cowork folder (Tasks › shared-property-co-buying-market-2026-11-18), which holds an older snapshot, the first prototype and project-juntos.zip. Also check on GitHub that the repository is private.
**Recommendation:** do it this week, outside working time, then write "ANSWER: deleted" here.
**If no answer:** the copies stay on work systems, which the employer's policy rules out (D-021, R-008).
**ANSWER (Sam, 2026-11-22, in Claude Code):** "F012: done."

### F-001 · Employer check · closed 2026-11-22
**ANSWER (Sam, 2026-11-22, in Claude Code):** selected "F-001 cleared, Build from today": "You confirm you've read the agreement and it has no exclusivity, approval or IP clause that blocks this." Recorded as D-023, Build mode from 2026-11-22.

**Checked on 2026-11-21** (Chief of Staff, with Legal & Compliance): your employer's side-project policy, its conflict-of-interest policy and its code of conduct, plus Spanish law. In short:
- Side projects need no sign-off unless your employment agreement requires it; there's no local supplement for Spain. A board or director role in a for-profit company needs your manager's pre-approval; you plan none.
- Not on work devices, accounts, systems or tools, and not in working time. Don't use the job to recruit colleagues or win customers, don't use the employer's confidential information or work, don't compete, and don't act as its supplier or customer (personal subscriptions to its products are fine).
- Your stake in the company is a financial interest: keep it away from decisions in your job, such as dealings with customers you cover, or disclose it through the conflict-of-interest process.
- Spanish law: a second activity is allowed unless it's unfair competition or you've agreed paid full-time exclusivity (Workers' Statute art. 21). Software written within your job, or on your employer's instructions, belongs to the employer (Intellectual Property Act art. 97.4); work outside your duties isn't covered by that rule.

**ANSWER (Sam, 2026-11-21, in chat):** "Ok so let's wrap-up this, and I'll move it to non-MS resources. I have no intention of being a director or anything - this is just an idea that's a thought experiment right now. I'm thinking of using a personal AI sub to build a product to see if it gains any interest, is all."
**Last step done:** Sam read his employment agreement; cleared on 2026-11-22 (answer above).

### F-007 · Where should the project live? (D-013) · closed 2026-11-21
**ANSWER (Sam, 2026-11-21, in chat):** move it to non-MS resources and use a personal AI subscription; his full message is quoted in F-001 and D-021.
**Recorded as:** D-021, which makes D-013. The move itself is F-012; the repository is D-022 (2026-11-22).
