# Maximum budget per profile (B-058, D-044)

_2026-11-23, Session 23. (Product with Builder; the privacy and equal-treatment notes by Legal & Compliance.)_

Sam (2026-11-23, in Claude Code): "profiles need to have a maximum € they can spend, which obviously combined with others will filter what houses they can afford."

## Summary
- Each profile has a **maximum budget**: the most that household would put towards a home's price, in euros, not counting taxes and fees. It is a step in the onboarding wizard, which won't move on without one, and a section of the profile.
- A group's **combined budget** is everyone's budgets added up. Its *Homes* view shows only the homes priced within the total, and says how many others that fit were hidden for price. If anyone hasn't set a budget yet (accounts made before this change), prices aren't filtered, and the group is told who still has to set one.
- **Homes, never people.** Budgets play no part in matching, scores, group suggestions or any list of people (AGENTS.md section 4; D-009; D-037). The code says so where the total is worked out (`Budgets` in `app/src/Compicasa.Core/Homes.cs`).
- **Private by default.** Only you see your figure. A group's members see the group's total, so in a group of two each can work out the other's; the profile's hint says so plainly. The Home page filters its three homes by you plus your best match, but doesn't show that pair's total, because the other person hasn't joined a group with you.
- Made-up people have made-up budgets (80,000 € a bedroom plus 50,000 to 170,000 €, steady for each id), so the demo filters by price. These are Assumptions, made up only for the demo.

## How it works
| Part | Rule | Where |
|---|---|---|
| Field | `maxBudget`, whole euros, 10,000 to 5,000,000, or empty | `Profile.MaxBudget`; checked in `ProfileRules.Clean` (`badBudget`) |
| Wizard | Step 7 of 9, "Your budget", after "Where and how long"; won't move on without a valid amount | `WIZ` and `SECTIONS.budget` in `wwwroot/app.js` |
| Total | Sum of everyone's budget; none if anyone hasn't set one | `Budgets.Combined` |
| Homes | A price over the total fails the fit (`miss: price`); a home with no price is kept and flagged "check the price" | `HomeFit.Fit(…, budget)` |
| Group's *Homes* | Returns `budget`, `overBudget` (homes that fit except on price) and `budgetUnset` (who hasn't set one) | `GET /api/groups/{id}/homes` |
| Home page | Filters by you plus your best match; returns only `budgetSet` (yours), not the pair's total | `GET /api/me/homes` |
| Shortlist | A shortlisted home now over the total shows "over your budget"; made-up members vote No, giving the price as the reason | `Shortlist.cs` |
| Guide | Step 1 and step 4 mention the budget | `helpSecs` in `wwwroot/i18n.js` |

The figure is for the price only. The house-voting estimate (D-043) puts taxes and fees at about 7.5% more in Madrid: 6% ITP (Verified on the Comunidad de Madrid's site, Session 22) plus about 1.5% for other costs (Assumption). The hint says "roughly another 7.5%".

## Before real users
- **Equal treatment (Legal & Compliance).** Socioeconomic status is a protected ground under Ley 15/2022. The lawyer brief (A3) asked whether we could ask "can you fund your share: yes / not sure" *without figures*. This asks for a figure, though it only ever filters homes. A question is added to A3: may we store a private maximum budget and use it only to filter homes, and may a group see its total? This joins the household-type gate (B-042) before anyone but Sam signs up (R-006).
- **Privacy (B-052).** A budget is financial data about a person. Add it to the data map and the draft privacy notice: what it is for, who sees it (only you; a group sees the total), and that it is deleted with the account.

## Open questions
- Should the total use the budgets of everyone invited, or only of those who have said yes? It now follows the group's other rules for its brief: everyone still in, including people invited who haven't answered yet.
- Would people rather give a share of the total (for example "up to 40%") than a figure? Not tested; no user evidence yet.
- Should the agreed-home cost estimate (D-043) warn you if your share is over your own budget? Proposed as a follow-up in B-058.
