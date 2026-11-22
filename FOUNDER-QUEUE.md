# Founder queue: what needs Sam

Agents add requests here. Sam replies by writing **ANSWER:** under an item. The CEO records each answer in DECISIONS.md, and the Chief of Staff closes the item. Silence, or a partial answer, approves nothing. "By" dates are suggestions.

## Open

### F-012 · Push to your private repository, then delete the work copies · by 2026-11-28
**Why:** your employer's policy rules out work systems for side projects, so the project moves to a private repository, project-juntos, on your personal GitHub account (D-021, D-022).
**Ask:**
1. Unzip project-juntos.zip on your personal computer. Use it rather than any earlier download of the OneDrive folder: it's the version set up for GitHub.
2. On your personal GitHub account, create a private repository named project-juntos, without a README, .gitignore or licence, so the first push goes in cleanly.
3. In the unzipped project-juntos folder, run these, with your personal name and email so no work address ends up in the history:
   - `git init -b main`
   - `git config user.name "<your name>"` and `git config user.email "<your personal email>"`
   - `git add -A` and `git commit -m "Import project files (sessions 1 to 7)"`
   - `git remote add origin https://github.com/<your-username>/project-juntos.git` and `git push -u origin main`
4. On GitHub, check the repository is private and its front page shows the README.
5. Delete the work copies: the "Co-buying startup" folder at the root of your work OneDrive, and this project's task folder in its Cowork folder (Tasks › shared-property-co-buying-market-2026-11-18), which holds an older snapshot, the first prototype and project-juntos.zip.
6. Start the first session with the first-session prompt in `KICKOFF.md`.

**Recommendation:** do this before any new session, so no more work happens on work systems. If you want certainty first, ask HR whether you may take these files; they hold only your own notes and public research.
**If no answer:** no session can start, as none may run on work systems.

### F-001 · Employer check: last step · by 2026-11-28
**Checked on 2026-11-21** (Chief of Staff, with Legal & Compliance): your employer's side-project policy, its conflict-of-interest policy and its code of conduct, plus Spanish law. In short:
- Side projects need no sign-off unless your employment agreement requires it; there's no local supplement for Spain. A board or director role in a for-profit company needs your manager's pre-approval; you plan none.
- Not on work devices, accounts, systems or tools, and not in working time. Don't use the job to recruit colleagues or win customers, don't use the employer's confidential information or work, don't compete, and don't act as its supplier or customer (personal subscriptions to its products are fine).
- Your stake in the company is a financial interest: keep it away from decisions in your job, such as dealings with customers you cover, or disclose it through the conflict-of-interest process.
- Spanish law: a second activity is allowed unless it's unfair competition or you've agreed paid full-time exclusivity (Workers' Statute art. 21). Software written within your job, or on your employer's instructions, belongs to the employer (Intellectual Property Act art. 97.4); work outside your duties isn't covered by that rule.

**ANSWER (Sam, 2026-11-21, in chat):** "Ok so let's wrap-up this, and I'll move it to non-MS resources. I have no intention of being a director or anything - this is just an idea that's a thought experiment right now. I'm thinking of using a personal AI sub to build a product to see if it gains any interest, is all."
**Still open:** read your employment agreement (HR can send you a copy) for an approval requirement, a full-time exclusivity clause or IP terms.
**If no answer:** F-001 stays open and sessions stay in Plan mode. You decide when it's cleared: write "F-001 cleared" with the date, then "Build mode from <date>" (AGENTS.md section 3).

### F-002 · Time and money for Phase 1 (about six weeks) · by 2026-11-27
**Options:**
- A) about 2 hours a week (12 hours in total), up to €300
- B) about 5 hours a week (30 hours in total), up to €1,000
- C) your own numbers

**Note:** the pilot plan needs about 24 hours of your time over six weeks, so option A works only if a delegate runs the interviews.
**Recommendation:** B. Finance costs the plan against your choice (B-008).
**If no answer:** no spending; research only.

### F-003 · The name (D-007) · within two weeks of F-001
**Recommendation:** keep Compicasa as the provisional favourite. Before approving or spending, run a clearance search for similar names in the services we'll actually offer (Compricasa, Compracasa, and the rival Casicasa), and a quick comprehension test with Spanish speakers. Brand & Marketing can do a first pass on the public registers now (B-024). Then buy compicasa.es and compicasa.com (€10.99 + €21.99 = €32.98 listed on 2026-11-20; prices and availability can change) and file a trademark for those services.
**If no answer:** "working name" everywhere; nothing published.

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
**Options:** A) confirm as written · B) stricter: read-only until F-001 · C) edit
**Recommendation:** A. Plan mode matches F-001's default (review and plan only), and every later mode needs your dated switch.
**If no answer:** Plan mode stays in force, and nothing moves to Build mode.

## Closed

### F-007 · Where should the project live? (D-013) · closed 2026-11-21
**ANSWER (Sam, 2026-11-21, in chat):** move it to non-MS resources and use a personal AI subscription; his full message is quoted in F-001 and D-021.
**Recorded as:** D-021, which makes D-013. The move itself is F-012; the repository is D-022 (2026-11-22).
