# Phase 1 validation plan: a hand-run matching pilot (B-002)

_Draft v0.1, 2026-11-20 · Validation, with Product and Legal & Compliance (AI) · For Sam's approval (F-010, D-018). **Nothing here runs until every gate in section 2 is met.**_

## Summary for Sam
- **What we test:** will strangers who plan to buy in the same Madrid area accept an introduction both ways, agree a joint brief, and commit as a group *before* seeing any home? The last point is our riskiest assumption (B-001, D-017).
- **How:** about 20 eligible households, pairs only, run by hand (forms, email and video calls, no app). No money, listings or advice. About six weeks once the gates are met.
- **Commitment without money:** a ladder of two meetings, a joint brief both approve, an exit-rules worksheet and an optional non-binding pledge. No deposit unless a lawyer clears it.
- **Your time:** about 24 hours over six weeks, which fits option B in F-002 (30 hours) but not option A (12 hours).
- **Your decisions:** approve this design (F-010), pick the area and price band (F-004) and approve the measures in section 8 (F-005). Earliest sensible start: Monday 11 January 2027, after Reyes, and only if the gates have cleared.

## 1. What we'll learn, and what we won't
| Question | Hypothesis | Measure (section 8) |
|---|---|---|
| Will strangers accept an introduction both ways? | H1, H2 (charter) | M1 |
| Will a matched pair agree a joint brief? | H2 | M2 |
| Will a pair commit as a group before seeing any home? | H3, early proxy; D-017 | M3 |

**Not tested here:** lending (B-009), the legal structure (Part B of the lawyer brief), willingness to pay (saying you'd pay isn't paying), and homes or listings (Phase 1 is brief-only, D-015).

## 2. Gates: nothing runs until all are met
| Gate | What it needs | Status on 2026-11-21 | Owner |
|---|---|---|---|
| F-001 | Positive employer clearance, in writing if needed | Policy checked on 2026-11-21; Sam's employment agreement still to read | Sam |
| F-007, D-013 | Where the project and the participants' data live (never on work systems) | Project: the private repository project-juntos (D-021, D-022). Participants' data: restricted storage chosen under B-013 | Sam |
| F-002 | Time and budget for Phase 1 | Open | Sam |
| F-004, D-008 | The first customer group, area and price band | Open | Sam |
| F-005, D-012 | The measures and decision rules in section 8 | Open | Sam |
| F-010, D-018 | Sam approves this design | Open | Sam |
| B-003, F-006 | The lawyer's written answers to Part A (status, data, equal treatment, safety, the pledge, claims) | Brief ready | Sam |
| B-013 | Privacy and safety groundwork: data map, lawful basis, retention, DPIA decision, incident process, named responder | Done 2026-11-22 (`research/2026-11-22-privacy.md`); still needed before go-ahead: a short DPIA (B-027), Sam's choice of tools, the lawyer's review | Legal & Compliance |
| B-020 | Spanish pilot materials, reviewed by the lawyer where needed and approved by Sam | To do | Product |
| B-006 | Interview guide in Spanish | To do | Validation |
| Go-ahead | Sam's dated approval to recruit and collect data (AGENTS.md §4) | Not given | Sam |

## 3. Design at a glance
- **Format:** a hand-run ("concierge") pilot. Spanish forms, email and video calls; no app.
- **Groups:** pairs of households only (D-008, option A). Groups of three or four come later.
- **Matching:** a person (Sam or a delegate he approves) checks every suggestion. Suggestions use the prototype's v0.3 rules on pseudonymised profiles, minus anything pending a lawyer: no household-type or money filters (D-009), and no "who I'll share with" dealbreaker until A3 is answered.
- **AI's role:** agents draft materials and analyse pseudonymised, aggregated results. They never see names or contact details, and work only on systems approved under F-001, F-007 and B-013.
- **Size and length:** about 20 eligible households; about six weeks; a check-in 30 days after the introduction window closes.

## 4. Who can take part (lawyer to check, A3)
**Eligible:**
- aged 18 or over (yes/no; no date of birth)
- planning to buy a home to live in, in the pilot area, within 12 months
- interested in the pilot's home price band (set under F-004; participants say whether it suits them); we don't collect income, savings or mortgage details
- open to buying with a household they haven't met
- accepts the code of conduct, privacy notice and participation terms

**Not in this pilot:** people who want only a room (D-008, option C, later) and people buying to let rather than to live in.

**Never criteria:** age beyond 18+, sex, nationality, ethnicity, religion, disability, health, household type or income.

**Area and price band (F-004):** Validation and Research propose two or three candidate Madrid areas (B-022), using public data only, no scraping. Criteria: enough three- and four-bedroom homes, a price band two households could plausibly share, and good transport.

## 5. Recruitment (after the gates; Sam approves channels and wording)
- **Target:** 20 eligible households with complete profiles. Recruit for two weeks, extendable once by a week.
- **Funnel assumption:** about 60 sign-ups if a third are eligible (*Assumption*; replace with real numbers).
- **Channels (pick two or three):**
  - Sam's personal network and friends of friends, never through work channels or colleagues (F-001)
  - Madrid housing, community and expat groups whose rules allow it
  - a Spanish landing page with a waiting list, under a neutral working name (B-007)
  - optionally, a small paid test (section 12), with no targeting by age, sex or other protected traits; check the ad platform's rules for housing ads first
- **Message:** honest. It's a free, hand-run pilot to see whether people match. There are no homes, no advice and no app.
- **Incentives:** none for joining. An optional small thank-you for interviews (section 12).

## 6. The participant journey
| Step | What happens | Who | When |
|---|---|---|---|
| 0. Sign up | Spanish form: eligibility, privacy notice, consent | Participant | Weeks 1–2 |
| 1. Profile | About 15 minutes; fields as in Annex A of the lawyer brief | Participant | Weeks 1–2 |
| 2. Welcome | One of two 30-minute group video sessions: how the pilot works, safety, questions | Sam | End of week 2 |
| 3. Introductions | Up to three anonymous introduction cards each: how they live, home needs, time horizon, why we suggest them and where they differ. No names, photos or ages | Sam checks; sent by email | Monday of week 3 |
| 4. Decide | Accept or decline within five days. An optional reason stays private. Anyone with more than one mutual acceptance picks one to continue with | Participants | Week 3 |
| 5. First call | 45-minute video call; Sam opens it (guidance, ground rules) and can stay or leave | Pair, Sam | Within 7 days of mutual acceptance |
| 6. Second meeting | Video, or in person in a public place | Pair | Within the 14-day window |
| 7. Joint brief | Template built from the prototype's home brief (bedrooms, bathrooms, useful-area range, must-haves, area, price band, timing; an illustrative room split). Both households approve it | Pair | Within the 14-day window |
| 8. Exit-rules worksheet | About ten discussion prompts: how long to stay, leaving, buy-outs, valuation, costs, deadlock, death, divorce or a new partner, guests and pets. Clearly not legal advice; a lawyer drafts any real agreement | Pair | Within the 14-day window |
| 9. Group pledge (optional) | Non-binding and signed by typing names: "we intend to search together for eight weeks using our brief". No money; anyone may withdraw. Only if the lawyer clears it (A5) | Pair | Within the 14-day window |
| 10. Follow-up | Short survey at day 14; interviews (section 9); a check-in 30 days after the window closes | Sam | Weeks 5–6, then +30 days |

Anyone can withdraw at any time, and their data is deleted on request. If round one leaves people with no mutual acceptance, one optional second round in week 4 offers up to two new introductions each.

## 7. Safety and conduct
- **Code of conduct:** respect; no discrimination, harassment or pressure; never ask for money, bank details or financial documents.
- **Safety guidance:** first meetings on video; in-person meetings in public places; tell someone where you are; never pay anyone; report concerns.
- **Named responder:** Sam, or a delegate he names. Reply within 24 hours; emergencies go to 112. Incidents are logged in the restricted storage, not in this repository.
- **Removal:** anyone who breaches the code leaves the pilot.
- **Verification:** no identity documents. The facilitated first call is a basic check that each person is real; the lawyer advises whether more is needed (A4).
- **Stop rule:** a serious safety incident or a data breach pauses the pilot at once while the B-013 process runs.

## 8. Measures and decision rules (for F-005 and D-012)
Thresholds are examples, not benchmarks. Always report the denominators.

| ID | Measure | Denominators | Continue if |
|---|---|---|---|
| M1 | Introductions accepted by both sides within 14 days of the first introductions | Introductions shown; households shown at least one | 6 or more |
| M2 | Joint briefs approved by both households | Mutually accepted pairs; pairs that held a first call | 3 or more |
| M3 | Pairs that complete the exit-rules worksheet and sign the pledge | Pairs with an approved brief; pairs offered the pledge | 2 or more |

**Also tracked, with no thresholds:** sign-ups; eligible households; completed profiles; households with no viable introduction; declines and grouped reasons; no-shows and cancellations; second meetings; days from acceptance to brief; pairs still together at the 30-day check-in; safety reports.

**Decision rules**
- **Continue** (design Phase 2, still subject to the lawyer's written review and a lending route): M1 and M2 met, and M3 met (2 or more).
- **Change and rerun a smaller round:** M1 met, but M2 or M3 missed. Change the brief process, the facilitation or the timing.
- **Rethink the idea:** no mutual acceptances after at least 15 eligible households have each seen two or more introductions.
- **Weak signal:** 1 to 5 mutual acceptances after at least 15 eligible households have each seen two or more introductions. Change the introductions and rerun once.
- **Inconclusive, not a "no":** fewer than 12 eligible households, or fewer than 15 households saw two or more introductions. Fix recruitment before concluding anything.

Interviews explain the numbers; they aren't success measures.

## 9. Interviews
- 12 interviews of 30 minutes in Spanish, using the guide (B-006). Consent each time; recording only with separate consent.
- **Sample:** about four people from pairs with an approved brief, four who accepted but didn't reach a brief, and four who declined or had no match.
- **Topics:** alternatives (renting, moving, waiting, buying with someone they know); why they accepted or declined; trust and safety; fears about exits; what they'd need before committing money; whether the brief helped; what they'd expect to pay (stated, so weak evidence).
- **Who:** Sam, or a delegate he approves.
- **Output:** pseudonymised notes, and themes in a dated research note.

## 10. Data handling (summary; B-013 has the detail)
- Real participants' data lives only in restricted storage Sam chooses (F-007, B-013): never in this repository or on work systems. This repository holds only pseudonymised IDs (H-01, H-02 and so on) and totals.
- Collect the minimum: Annex A of the lawyer brief. No date of birth, photos, identity documents, nationality, income or details about children.
- Proposed retention: delete within 90 days of the pilot ending, except for people who ask to hear about next steps. The lawyer and B-013 confirm.
- People can see, correct or delete their data, and withdraw, at any time.

## 11. Timeline once the gates clear
Dates assume a start on Monday 11 January 2027, after Reyes (Wednesday 6 January). Don't recruit over Christmas.

| When | What |
|---|---|
| Before the start | B-020 materials, storage and forms set up; a dry run with two or three friendly testers, not counted |
| Weeks 1–2 (11–24 Jan) | Recruitment, sign-ups and profiles; welcome sessions at the end of week 2 |
| Week 3 (from Mon 25 Jan) | Introductions sent; decisions due by Friday 29 January |
| 25 Jan – Sun 7 Feb | The 14-day window: first calls, second meetings, briefs, worksheets, pledges |
| Weeks 5–6 (8–21 Feb) | Interviews, analysis and report; Sam decides by Friday 19 February |
| Tue 9 Mar | 30-day check-in |

## 12. Time and money
**Sam's hours (estimate)**

| Task | Hours |
|---|---|
| Approving and reviewing materials | 2 |
| Two welcome sessions of 30 minutes | 1 |
| Checking suggested introductions | 2 |
| Opening first calls (up to 8 pairs × 45 minutes) | 6 |
| Responder duty and emails (30 minutes a week × 6) | 3 |
| Interviews (12 × 30 minutes) plus notes (12 × 15 minutes) | 9 |
| Reviewing results and deciding | 1 |
| **Total** | **24** |

That fits option B in F-002 (30 hours). Option A (12 hours) is too tight: even a slim version needs about 13 hours, with 6 interviews, joining only the first 10 minutes of each first call, a 1-hour matching check and 20 minutes a week of responder duty. Under option A, a delegate would need to run the interviews.

**Money (none of it approved; VAT included where known)**

| Item | Low (€) | High (€) | Notes |
|---|---|---|---|
| Lawyer, Part A (F-006) | 363 | 726 | *Assumption*: €300–600 before VAT, plus 21% VAT. Replace with real quotes |
| Form, storage and video tools for two months | 0 | 40 | *Assumption*: free tiers, or about €20 a month; B-013 sets the requirements (EU hosting, processor terms); Sam chooses the tools |
| Recruitment ads (optional test) | 0 | 200 | Only if Sam approves |
| Interview thank-yous (optional) | 0 | 180 | 12 × €15 vouchers |
| Domains (only after F-003) | 0 | 32.98 | GoDaddy listing on 20 Nov |
| **Total** | **363** | **1,178.98** | |

The high case is €178.98 over option B's €1,000 cap. Dropping the optional ads and vouchers brings it to €798.98.

## 13. Who does what
| Role | Work |
|---|---|
| Validation (owner) | This plan, the experiment cards, measures, analysis and the report |
| Product | Spanish materials (B-020): forms, introduction card, joint-brief template, exit-rules worksheet, pledge |
| Legal & Compliance | Privacy notice, consent texts, participation terms, code of conduct, safety guidance (with B-013); turns the lawyer's answers into changes |
| Builder | Forms, and a pseudonymised matching sheet that applies the v0.3 rules without household-type or money filters |
| Tester | Checks the forms and the matching sheet with made-up profiles |
| Brand & Marketing | Recruitment wording under a neutral working name |
| Finance | Costs and the ledger (`project/budget.md`) |
| Chief of Staff | Schedule, founder queue, log |
| Sam | Approvals, recruitment posts, first calls, interviews, responder duty; holds the participants' data |

## 14. Experiment cards
Logged as planned in `project/experiments.md`. Fill in the results and decisions after the pilot.

### EXP-001: Two-way introductions
- **Hypothesis:** We believe households planning to buy in the same Madrid area and price band will accept an introduction to a compatible household they haven't met, because they can't afford a suitable home alone.
- **Test:** each eligible household sees up to three anonymous introduction cards and decides within five days; identities are shared only after both accept.
- **Metric and thresholds:** M1. Six or more means continue. None after at least 15 households have each seen two or more introductions means rethink.
- **Sample and duration:** about 20 eligible households; a 14-day window after two weeks of recruitment.
- **Cost and approvals:** within section 12; recruitment, contact and data collection need every gate in section 2 and Sam's separate dated go-ahead; F-010 approves the design only.
- **Data and privacy:** Annex A of the lawyer brief; B-013; restricted storage; pseudonymised here.
- **Result:** _to fill in_
- **Decision:** _to fill in_ (feeds D-012 and the gate to Phase 2)

### EXP-002: A joint brief both households approve
- **Hypothesis:** We believe a matched pair will agree a joint brief within 14 days, because the brief turns two profiles into one concrete plan.
- **Test:** after a first call and a second meeting, each pair completes the joint-brief template, and both households approve it.
- **Metric and thresholds:** M2. Three or more means continue; fewer means change the brief process and rerun.
- **Sample and duration:** the mutually accepted pairs from EXP-001; the same 14-day window.
- **Cost and approvals:** as EXP-001.
- **Data and privacy:** briefs hold no names; stored with the pair's pseudonymised ID.
- **Result:** _to fill in_
- **Decision:** _to fill in_

### EXP-003: Committing as a group before seeing a home, with no money
- **Hypothesis:** We believe some pairs will commit to searching together before seeing any home, because agreeing the brief and the exit rules first builds trust (D-017).
- **Test:** pairs with an approved brief complete the exit-rules worksheet and are offered the non-binding pledge, if the lawyer clears it (A5). No listings are shown.
- **Metric and thresholds:** M3. Two or more means continue. None means rethink the group-first claim. Pairs still together at the 30-day check-in are also tracked.
- **Sample and duration:** pairs with an approved brief; the same window, then the 30-day check-in.
- **Cost and approvals:** as EXP-001; no money changes hands.
- **Data and privacy:** worksheets and pledges kept in restricted storage; only counts here.
- **Result:** _to fill in_
- **Decision:** _to fill in_

## 15. Open questions
- Which area and price band (F-004, B-022)?
- Can "who I'll share with", household size and the number of children be used or shown (lawyer, A3)?
- Is the pledge safe to offer (lawyer, A5)?
- Does Sam open every first call, or does a delegate?
- Should participants see a Spanish version of the prototype to explore their brief (B-018), or is that a later round?
