# Strategy: a free tool first (v1.0, 2026-11-24)
_(CEO, Session 27, after Sam's decision D-048. Evidence: `research/2026-11-24-problem-evidence.md`, `research/2026-11-24-competitors-refresh.md`, `research/2026-11-24-free-launch-rules.md`. Updates `project/charter.md` where they differ. Sections 4 to 6 contain proposals for Sam: F-032 to F-035.)_

## Summary
- **Problem:** in Spain a home costs about 8 years of household income and needs €61,000–70,000 in cash up front, so people on one income, and most under-35s, can't buy alone. Buying with others is legal and halves or thirds the cash each person needs, but there's no safe, simple way to find the right co-buyers, agree what to buy and decide together.
- **Evidence:** the affordability problem is strong and Verified in official data. Demand for the remedy is not: nobody has measured willingness to buy with strangers, sign-ups convert badly (habitacion.com under 1%), and abroad every platform that matched strangers for a main home has closed, stalled or turned to friends.
- **Competitors:** nobody in Spain lets strangers form a group, agree a brief and choose a whole home together. Casicasa stops before forming groups; habitacion.com sells rooms in homes it picks. Our gaps: no exit or agreement tool, no lending route, no completed purchase.
- **Strategy:** release Compicasa free, for people buying with strangers *or* with people they already know; make it useful to one person or one group before matches exist; put the little promotion we can do into one place (Madrid, to live in); and judge it by groups that agree on a home, not by sign-ups. Matching stays free for good; any later charge goes on the agreement-and-exit layer, delivered with lawyers.
- **Before anyone else signs up:** finish the privacy and safety basics and the operator notice (section 4). Costs stay near zero until interest is proven: no company and no lawyer until we charge (D-050). Hosting stays on the Visual Studio subscription for now (D-049), though its terms say development and testing only; a move stays ready.

## 1. Problem statement
> People in Spain who can't afford a home on their own, because the deposit and taxes alone are years of saving on one income, have no safe and simple way to pool with others: to find co-buyers they can trust, agree what home they need, choose it together and know what each pays and how anyone can leave.

Who feels it most: single-income households (28.4% of households), under-35s (ownership down from 41% in 2007 to 22% in 2025) and renters, whose rent eats the savings a deposit needs. Pooling attacks the binding constraint, the cash at the start: two co-buyers each need about 11–12 years of saving instead of 21–25, and three about 7–8 (calculation in the problem-evidence note).

What we don't claim: that many people already want to buy with strangers. That is the hypothesis this release tests.

## 2. Research backing (detail and sources in the three notes)
| Strength | Evidence |
|---|---|
| **Strong** (official data) | Prices +12.2% in a year (INE, Verified); leaving home at 30.2 against 26.3 in the EU (Eurostat, Verified); 26.8% of market-rent tenants pay over 40% of income against 18.5% in the EU (Eurostat, Verified); 5.6 million one-person households (INE, Verified); a shortage of about 750,000 homes (Banco de España, Reported); housing the top concern in the CIS all year (Reported) |
| **Moderate** (press, consistent) | 8 years of household income to buy, 15 years of one salary in Madrid; renting alone takes 98.7% of a young net salary; brokers report rising enquiries about buying with friends or strangers (all Reported) |
| **Weak or against** | No Spanish data on buying with non-partners. Abroad, 30–60% say they'd consider it but 5–15% do it, and US friend co-buying fell from 14% to 5% of buyers (2023–2025). Waiting lists convert under 1%. Stranger-matching platforms: Kohab closed, Husmates matched two groups. iAhorro: "no es una operación que se suela hacer" (all Reported) |
| **Cuts both ways** | Tenant overburden has fallen from 43.3% in 2015 (Verified); say so whenever we cite it |

So the free release must produce the evidence nobody has: how many people, once they can, actually form a group and agree on a home.

## 3. Competitors, and where we do better
| | Compicasa | Casicasa | habitacion.com | Rent-to-own (Libeen, Gradual, Wannaprop) | CoBuy (US) |
|---|---|---|---|---|---|
| Find co-buyers you don't know | **Yes**, open matching, groups of 2–4 | No (private groups it prepares) | No (buy a room) | No | No |
| Bring people you know | Not yet (proposal B-075) | Yes | Group option for friends | No | Yes, its whole model |
| Whole home the group chooses | **Yes** | Previews of listings | No: the company picks | One household picks | Yes |
| Joint brief and group votes | **Yes** (brief, three-round hidden voting) | No | No | No | Decisions after purchase |
| Cost per person with regional tax | **Yes**, every region's ITP Verified | Indicative share, no tax | Price set by the company | n/a | No (US) |
| Agreement and exit | Questions for the lawyer only | Guides | Notarised pact, buy-back | Purchase option | **Yes, its paid product** |
| Price to users | **Free** | Free | Margin built into the price | Entry fees | $1,000 a year a group |

**Where we can do better:**
1. **Open group formation**: the one thing no Spanish rival does. Keep it, but don't depend on it alone.
2. **Usefulness before matches**: the cost-per-person calculator with real regional tax, the joint brief and the shortlist work for a group of friends today. That covers the proven segment (friends and relatives) and fills the empty-room problem every matcher hit.
3. **Honesty as a feature**: free, no margin, open about where homes come from (D-046) and, later, exact fees. No rival publishes fees.
4. **The exit layer, later**: CoBuy shows co-owners pay for the agreement and decisions after the purchase. We can't give legal advice, so this means a structured worksheet plus partner lawyers (section 6).

**Where we are behind:** no lending route (R-002), no agreement or exit product, no completed purchase, an unknown brand, and no human support beyond Sam.

## 4. Strategy
**Stage 1: ready to open (now until the release checklist is met).** Nothing opens to the public until every item is done or Sam waives it in writing (F-033):

| # | Item | Backlog / ask |
|---|---|---|
| 1 | Hosting: stays on the Visual Studio subscription for now, Sam's choice (D-049), with a move to a regular subscription kept ready | D-049, B-073 |
| 2 | Sam runs it as an individual (D-050); the operator notice that follows | F-037, B-072 |
| 3 | No paid lawyer before interest is proven (D-050). Instead, conservative defaults from our own research (`research/2026-11-24-free-launch-rules.md`): free, no fees, no advice, no filtering people by protected characteristics, household or money. The lawyer comes back before charging, or if a question blocks the release | D-050 |
| 4 | No household-type filter on real people (done, B-042); the budget filters homes only, never people (D-044) | B-042 |
| 5 | Privacy notice and data map for everything we now hold; a short DPIA; record of processing. **Drafted 2026-11-28** (`research/2026-11-28-dpia.md`); Sam signs off (F-055) | B-052, B-027 (done) |
| 6 | DSA basics: reports reviewed by a person, reasons given, a contact point, final terms. **Done 2026-11-28** (D-062) except the operator notice (F-037) | B-041, B-072 (done) |
| 7 | Delete an account fully (including the sign-in record); download your data. **Done 2026-11-28** (D-062); the sign-in record removal needs Sam to run one script (F-054) | B-039 (done) |
| 8 | Safety: named responder (Sam), code of conduct (built), safety guidance | F-033 |
| 9 | Measurement of the funnel, counts only, no tracking cookies | B-074 |
| 10 | A native speaker's review of the Spanish | B-031 |
| 11 | Remove the invite code and `noindex`; a domain is optional (about €33 a year; the free `azurewebsites.net` address works) | Sam (F-014), at release |
| 12 | Made-up people off (`People:Demo=false`, D-059) and the preview's wording about them replaced. **Done on production 2026-11-28 (D-061)**; the test slot keeps them | B-097, B-098 (both done) |

**Stage 2: open, free, one place first.** The app already covers all of Spain; keep it open everywhere, but put promotion into Madrid, buying to live in (D-008's first group), so matches have enough people nearby. Be honest with newcomers about how many people there are: no made-up people, no padded numbers; while few compatible people are near them, say it's early days and tell them when someone joins, rather than lead with a count (D-059, `product/2026-11-27-early-days.md`). Every public step (posts, press, groups, ads) is Sam's to approve. Low-cost channels, roughly in order: press (buying with strangers is already a news story, and a free tool is a hook); a public, indexable cost calculator per region (a search magnet no rival has); Spanish and expat community groups; and people inviting the co-buyers they already know (B-075).

**Stage 3: measure.** One main measure: **groups that agree on a home** (all members vote yes in the final round) per month. Around it: completed profiles; share with a mutual match within 14 days; groups formed; groups with an agreed brief; and later, purchases by groups formed in the app (asked, never assumed). Sign-ups are not traction.

**Suggested milestones** (all Assumptions; Sam decides later, F-035). Counted on finished profiles in the focus area, never sign-ups, which convert badly (habitacion.com: under 1% of its waiting list bought). The ratios are a first guess, to be replaced by our own numbers after the first month (`#funnel`, B-074):

| Milestone | When | Numbers (focus area) | What it would show |
|---|---|---|---|
| **Signal** | first 90 days of real promotion | 300 finished profiles; 20% with a real mutual match within 14 days; 5 groups formed; 1 group agrees on a home | People will match with strangers at all. Below this, the pivot rule |
| **Traction** | within about 6 months | 1,000 finished profiles; 40% matched within 14 days; 30 groups formed and 10 agreeing on a home in a quarter | A newcomer usually finds someone; groups get as far as a home |
| **Critical mass** (time to look at charging) | when it happens | 3,000 finished profiles; 60% matched within 14 days; 25 groups forming a month; 3 purchases by groups formed in the app; a second city growing without promotion | It grows without Sam pushing it, and groups actually buy, so a paid agreement-and-exit pack would have buyers |

**Pivot rule:** after 90 days of real promotion, if under 10% of completed profiles get a mutual match, or no group agrees a brief, change course to a friends-first co-buying toolkit, or stop.

**Stage 4: charge, only after critical mass (D-010 stays open).** Matching stays free for good. A company and a lawyer come first (D-050). Options to test then:
1. An agreement-and-exit pack: a worksheet of the group's own rules, drafted into a contract by partner lawyers for a published fixed fee (CoBuy's model; we never give the advice ourselves).
2. Referral from free mortgage brokers who will take groups (credit intermediary rules apply).
3. Lead fees from agencies (likely makes us an intermediary: anti-money-laundering duties, regional registers: R-003).

**Costs: as close to zero as possible until interest is proven (D-050).** Hosting on the Visual Studio credit (D-049); no company and no lawyer; a domain optional (€32.98 listed, 2026-11-20). The real cost is Sam's time: moderation, safety replies within 24 hours, support.

## 5. Risks (full register: `RISKS.md`)
| Risk | Score | Response |
|---|---|---|
| R-001 strangers won't co-buy (international evidence now weighs against) | 9 | Free release measures it cheaply; friends-first fallback (B-075); pivot rule |
| R-002 banks won't lend to unrelated groups | 9 | Broker check (B-009) before groups reach an offer; say so in the app |
| R-025 interest doesn't convert: sign-ups without groups or purchases | 6 | Judge by groups agreeing on a home, never sign-ups |
| R-024 hosting breaks the Azure credit's terms, or the credit ends | 6 | Accepted for now (D-049); keep the move ready (B-073) and do it if the credit or the terms bite |
| R-026 running a public platform as an individual: published address, fines, DSA duties with one moderator | 6 | Accepted for now (D-050); conservative defaults; minimal back office (B-041); a lawyer and a company before charging |
| R-027 no path to revenue; charging later makes us an intermediary | 6 | Plan the paid layer around the agreement; lawyer maps each option |
| R-013 too few people in one place | 6 | One focus area; useful before matches; honest counts |
| R-006, R-015, R-007 discrimination, safety between strangers, data | 9, 6, 6 | Release checklist items 4 to 8 |
| R-028 costs and Sam's time grow with users, with no income | 4 | Free tiers where allowed; caps; the pivot rule |

## 6. What this changes
- **Validation route:** the free release replaces the hand-run, 20-household pilot (`product/phase1-validation-plan.md`) as the main test. The plan's safety rules carry over. F-035 asks Sam to retire F-004, F-005 and F-010 in its favour.
- **Modes:** Pilot mode's gate becomes the release checklist in section 4 instead of section 2 of the pilot plan. Changing the modes is Sam's (AGENTS.md section 11): F-033.
- **Mission:** unchanged, apart from F-031 (renting out).
- **Roadmap:** `project/roadmap.md` v0.3 follows these stages.
