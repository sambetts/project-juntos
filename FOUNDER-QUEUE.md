# Founder queue: what needs Sam

Agents add requests here. Sam replies by writing **ANSWER:** under an item. The CEO records each answer in DECISIONS.md, and the Chief of Staff closes the item. Silence, or a partial answer, approves nothing. "By" dates are suggestions.

## Open

### F-015 · One Azure region for the PoC: how? · by 2026-11-29
**Why:** you asked for everything in one European region (D-031). On 2026-11-22 no EU region offered both free tiers on your subscription. France Central has the free database but no free web-app quota; West Europe has the free web app but accepts no new databases. For now the web app is in West Europe and the database in France Central, the next region over, at €0. It works, and all 24 browser checks pass.
**Options:**
- A) Keep it as it is: €0, two neighbouring regions.
- B) Ask Microsoft for free web-app (F1) quota in France Central, then I move the web app there: one region, €0. You file it in the Azure portal (Quotas › App Service › France Central › F1 › 1). It's free and often approved quickly, but that isn't guaranteed.
- C) One region now, not quite free: the database on SQL Basic in a region with F1 (for example Sweden Central), about €4.40 a month before VAT.

**Recommendation:** B, keeping A until it's approved.
**If no answer:** A stays.


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

### F-006 · Send the lawyer brief and book Part A · by 2026-12-04, after F-001
**Why:** the pilot's data, matching rules and safety terms depend on the answers, and later the legal structure and revenue model too (D-009 to D-011).
**Ready:** `research/2026-11-20-lawyer-brief.md`. Its first part is for you only (a checklist); section 8 is the Spanish cover email.
**Ask:** from a personal email address, send it to two or three lawyers or firms (Partnerships can shortlist them: B-021) and compare fixed-fee quotes. Then book Part A: one hour plus a short written note. Part B is a quote only for now.
**Recommendation:** choose someone who covers property law and data protection, and approve the fee through F-002.
**If no answer:** the legal questions stay open, and nothing with real users goes ahead.

### F-008 · Co-founder or advisers? · by 2026-12-18
**Ask:** do you know a Spanish property lawyer, an estate agent or a mortgage broker who might advise? Answer only if you know someone: name two or three, in chat or here, and Partnerships drafts the outreach for your approval. We record only their organisation.
**Recommendation:** answer only if a name comes to mind; the lawyer shortlist (B-021) covers the rest.
**If no answer:** Partnerships researches candidates without contacting anyone.

### F-009 · Amend the listings decision (D-015) · by 2026-12-04
**Why:** the review found that links pasted by members lower the scraping risk but aren't permission, and Idealista's terms make no exception for them.
**Recommendation:** accept. Phase 1 is brief-only, with no listing links; later, only licensed or authorised links.
**If no answer:** D-004 stands, but the Homes tab (B-014) stays parked.

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

### F-014 · Ten-minute trademark search for Compicasa, then the domains · by 2026-12-06
**Why:** the public checks found no blocker (`research/2026-11-22-name-clearance.md`): no web use, no company, and .com and .es look free. But the trademark registers (OEPM and TMView) block automated searches, so a person has to run them. The closest sound-alike is Comprarcasa, an estate-agency network.
**Ask:** run the searches in that note's "Steps" section (about ten minutes), or name a delegate. Paste the results here.
**Then:** if nothing similar shows up in classes 35, 36, 42 or 45, approve buying compicasa.es and compicasa.com (€32.98 listed on 2026-11-20; re-check the price), from a personal account.
**Recommendation:** do the search this fortnight; buy only after it's clean. File a trademark later, with the lawyer.
**If no answer:** Compicasa stays a provisional working name; nothing is bought or published.

## Closed

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
