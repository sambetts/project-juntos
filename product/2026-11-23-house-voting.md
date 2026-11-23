# House voting for a group (B-056, D-043)

By the Product and Builder roles, Session 22, 2026-11-23. Sam asked: "A group should be able to add candidate houses, and each member be able to vote on it. When all members agree on a house then we should take some sort of action".

## Summary
- Members who have joined a group add homes from its *Homes* list to a **shortlist** (up to 20). Only homes from our index, so D-035 still holds: facts, the agency's name and the link to its advert.
- Each home goes through **three rounds**: view it, make an offer, go ahead. A round passes when everyone still in the group says **yes** at the current price. Each vote is Yes, Maybe or No, and Maybe or No asks for a reason (price, area, size, condition, "just a feeling", something else) that the voter can skip.
- **Votes on a round are hidden until everyone has voted**, so nobody just follows the first vote. A **price change** or a **newcomer** means everyone votes again. Rounds are worked out from the votes each time they're asked for, not stored.
- When everyone agrees, the group gets an **"agreed" pack**: what it might cost each of them, questions for their lawyer and notary, and a draft email to the agency. The draft is for **made-up homes only**: for real agencies it waits on Sam (F-024).
- What we never do: give legal, tax or financial conclusions (AGENTS.md section 4); send anything ourselves; contact a real agency.

## How it works
| Part | What | Where |
|---|---|---|
| Data | `Candidates` (group, home, who added it, price when added) and `CandidateVotes` (one per person, round and price); migration `Shortlist` | `app/src/Compicasa.Api/Shortlist.cs`, `Data.cs` |
| API | `GET/POST /api/groups/{id}/shortlist`, `DELETE …/{cid}` (whoever added it, or the group's proposer), `PUT …/{cid}/vote` | `Shortlist.cs` |
| Made-up members | Vote at once by how well the home fits the group's brief: 80 or more, yes; 65 to 79, mostly yes, sometimes maybe ("size"); under 65, maybe or no. The same result every time | `Shortlist.DemoVote` |
| Notifications | "added a home", "everyone wants to view it", "everyone wants to make an offer", "your group agrees on a home"; each opens the shortlist | `NoteKind`, `noteLink` |
| Front end | A fifth view on a group's page, *Candidatas* / *Shortlist* (`#group-<id>-shortlist`); "Add to shortlist" on each home; a tile and a next step on *Overview*; a section in the guide | `wwwroot/app.js`, `i18n.js`, `app.css` |
| Clean-up | Shortlists and votes go when an account is deleted or the daily batch of made-up people is replaced | `Shortlist.ForgetAsync` |

## The cost estimate: inputs and labels
Savings per person = share × price × (deposit + ITP + other costs); mortgage per person = share × price × (1 − deposit). Shares are equal, or follow the brief's room plan ("By rooms").

| Input | Value used | Label and source |
|---|---|---|
| Deposit | 20% | **Assumption** (common bank practice of lending up to 80% of price or valuation); no primary source checked this session |
| ITP, resale home, Comunidad de Madrid | 6% | **Verified**: [Comunidad de Madrid, Transmisiones Patrimoniales Onerosas](https://www.comunidad.madrid/atencion-contribuyente/transmisiones-patrimoniales-onerosas), "Tipos de gravamen", page updated 2026-11-03, checked 2026-11-23: "se aplicará el 6 por 100", with the example of a second-hand home. Secondary sources cite art. 28 of Decreto Legislativo 1/2010 (not read). The same page lists lower amounts for some buyers: a 10% reduction of the tax for a main home worth up to 250,000 €, 100% for under-35s in towns under 2,500 people, and a 4% rate for large families; the estimate ignores these and says so |
| Notary, registry, gestoría, valuation | about 1.5% | **Assumption**, no source |
| New homes (IVA plus AJD instead of ITP) | not handled | Our index holds resale adverts from agencies; the legal landscape note covers the difference (`research/2026-11-18-spain-legal-landscape.md`) |

Example checked in the browser test: a 317,000 € home split 50/50 gives savings of 87,175 € in total (27.5%) and a mortgage of 253,600 € (80%).

## Open questions
- Should the lawyer confirm the questions list, or fold it into the lawyer brief (B-003, F-006)?
- Is unanimity right for the "view it" round, or would a majority be enough to book a viewing? Test with people (Phase 1).
- Confirm the deposit and typical fees from primary sources before real users see the estimate (the ITP rate is Verified, D-043, Session 22).
- Emails to real agencies: Sam's decision (F-024).
