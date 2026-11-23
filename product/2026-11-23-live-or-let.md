# Live in it or rent it out (B-063)

_2026-11-23, Session 26. (Product), for the Builder. Sam asked for it (2026-11-23): "add the ability to filter for/set in profile that you're looking for either a home to live in or a home to rent out. It's a big differenciator that will be very important - if it's to rent, then a lot of the other filters won't apply so much"._

## Summary
- Every profile gets a **purpose**: *Para vivir* (live in it, the default) or *Para alquilar* (rent it out). It is asked in the onboarding wizard as step 2 and can be changed in the profile.
- **People match only with the same purpose, as a hard rule, checked both ways.** A group buys one home for one purpose: a home that some members live in and others let can't be both, and the two need different agreements (who lives there and pays what, against how rent is shared), different searches and, often, different finance and tax. Mixing would also make every living-together answer meaningless for half the group.
- **Letting is a shorter path.** The questions about living together (bedrooms for your household, private bathroom, parking, pets, daily habits, smoking, who you'd share with) are not asked and play no part in matching. Letters answer: who's buying, the home to let (bedrooms, up to how many co-owners), where, type, how long they'd keep it, budget and the home's features.
- **The home brief for a letting group** is the home itself: at least the largest number of bedrooms anyone asked for, a size from the usual formula, areas, types and must-haves; no room split, placement tips, parking or desks. Agreement starters become letting topics (kind of letting, who manages it, how rent and costs are shared, decisions, moving on), as prompts for a conversation and the lawyer, never as advice.
- **Existing accounts** become *Para vivir*, as do the 14 prototype people (parity unchanged). About a quarter of each day's generated made-up people are *Para alquilar*.

## The setting
| | Live in it (`live`) | Rent it out (`let`) |
|---|---|---|
| Spanish / English label | Para vivir / To live in | Para alquilar / To rent out |
| Default | Yes: new and existing profiles | Chosen by the person |
| Who sees it | Everyone who can see the profile (it isn't private: it decides who you can match with) | Same |

Purpose is not a protected characteristic (Ley 15/2022 lists none of this kind) and it says nothing about money or household type, so it may filter people (Assumption, to confirm in the lawyer brief, Part C). It adds no filter on protected characteristics, money or household type; D-009 and D-044 are unchanged, and the maximum budget still never feeds matching, scores or any list of people.

## Wizard and profile
| Step / section | Live (10 steps) | Let (8 steps) |
|---|---|---|
| Name and intro | Yes | Yes |
| **What the home is for** (new) | Yes | Yes |
| Who's buying (household) | Yes, with examples | Yes, no examples (they describe living together) |
| Your private space (bedrooms for you, bathroom, parking, pets) | Yes | No |
| How you live (sliders, home working, smoking) | Yes | No |
| Who you'd share with (households, cap, pets, smokers) | Yes | No |
| **The home to let** (new: bedrooms the home should have, up to how many co-owners) | No | Yes |
| Where and how long | "How long you plan to live there" | "How long you'd keep it" |
| Budget | Yes | Yes |
| Home features | Yes | Yes |

Answers to the skipped sections are kept, unused, so switching back restores them.

## Matching
| Rule | Live pair | Let pair | Mixed pair |
|---|---|---|---|
| Same purpose | — | — | **Dealbreaker, both ways**: "is buying to rent out; you, to live in" |
| Who you'd share with (household type) | Dealbreaker (demo, D-009) | Not applied | — |
| Smoking, pets | Dealbreakers | Not applied | — |
| Area and home-type overlap | Dealbreakers | Dealbreakers | — |
| Combined bedrooms ≤ 6 | Dealbreaker | Not applied (bedrooms are for the whole home) | — |
| Maximum households (group check) | Applies | Applies | — |
| Score | How you live 0.50, place 0.20, horizon 0.20, priorities 0.10 (unchanged) | **Place 0.40, horizon 0.40, priorities 0.20** (uncalibrated, Assumption) | — |
| Why you click / what to talk about | Unchanged | Same horizon, shared priorities; different horizons | — |

Made-up people's simulated replies use the same rule (score ≥ their pickiness, no dealbreaker). Changing purpose breaks any group you're in, as any dealbreaker does, and the group page says why.

## People
The profile setting is the filter: *People* shows only people with your purpose, and its header says "Looking for a home: to live in · Change". People with the other purpose are among those hidden by dealbreakers, each with the reason. There is no separate toggle to browse the other purpose, because you couldn't match with them; switching is one tap in the profile.

## Home brief, homes and shortlist for a letting group
- **Brief:** bedrooms = the most anyone asked for (each answer is "at least"); bathrooms 1 (Assumption: no requirement); no parking, desks, kids or room split; size = (bedrooms × 12 + bathrooms × 5 + 42) × 1.15, ±10%; areas, types and must-haves as for living.
- **Homes:** found and filtered exactly as now (bedrooms, type, area, size, must-haves, combined budget). A line says the rules for letting a home differ by region and town and are a question for the lawyer.
- **Shortlist:** cost estimate split equally only (no rooms to split by); the lawyer questions add four letting questions (kind of letting and any licence, sharing rent and costs, who manages it, tax on rental income).
- **Agreement starters:** kind of letting, managing the letting, rent and costs, decisions, moving on.

## Made-up data
The 14 prototype people stay *live* (the prototype has no purpose). Each day's 30 generated people: about one in four is *let*, decided from the id so their other answers stay as they were, with a letting wish in their bio.

## Open questions
- Whether letting groups need finance questions (rental yield, buy-to-let mortgages) before we show any; not before a lawyer and B-043.
- Whether a "both" or "not sure" purpose is worth adding after real users; not now (it would bring mixed groups back).
- The letting score weights are a first guess; tune after the pilot.
- Legal questions for the lawyer: `research/2026-11-20-lawyer-brief.md`, Part C (section 4A).
