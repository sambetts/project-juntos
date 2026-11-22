# Profile and matching model, v0.3 (2026-11-20, after the B-015 prototype fixes)

## Sam's original examples
- "I'm single (or married with kids), want a house with 3 rooms. I can live with other people in my situation. I need a driveway. I'm willing to share 50% of all the house."
- "I want just a room. I can live with whoever."

The service should find both the people and the homes that fit.

## Profile fields
- Household type: single, couple, family, single parent. It shapes the home brief and isn't scored. Whether it may filter real people waits for D-009.
- Bedrooms wanted, desks or workspaces, and whether a second living space is needed.
- Who they'll live with (D-009 decides whether this can filter real people).
- Share: a percentage of the whole home, or "just a room".
- Must-haves (for example a driveway or parking, outdoor space), accessibility needs (lift, step-free access) and a wish-list.
- Acceptable areas and home types.
- How they live: schedules, guests, noise, tidiness, cooking, smoking, pets.
- Time horizon: how long they plan to stay.
- Pickiness: how good a match has to be before they say yes.
- Later: budget, deposit, mortgage pre-approval and purchase window (subject to D-009 and a lawyer's review).

## Design principles
1. Groups, not just pairs (2–4 households); an anchor buyer starts a group (D-002).
2. Real matches need explicit opt-in from both sides. Groups form only from accepted matches, everyone accepts each other, and every member approves the joint brief.
3. Match on objective living preferences, never on protected characteristics. Household type and money don't filter or rank real people until D-009 and a lawyer's review.
4. Occupancy and accessibility needs shape the home brief, not who you're matched with.
5. Explain every match: why people fit and where they differ. Keep the other side's rejection reasons private.
6. The profile produces non-binding discussion prompts for a lawyer-reviewed agreement. It isn't the agreement.
7. A tie-break rule for disagreements, agreed up front.

## Rules as built in the prototype (v0.3, made-up profiles only)
- **Dealbreakers, checked both ways:**
  - who each side will share with (a demo preference, pending D-009)
  - smoking and pets
  - area overlap and home-type overlap
  - combined bedrooms of 6 or fewer (a demo cap)
  - You see your own reasons; the other side's reasons stay private.
- **Demo score (uncalibrated weights):** how you live 0.50, where and what type of home 0.20, time horizon 0.20, shared priorities 0.10.
  - Household type isn't scored.
  - Shared priorities leave out accessibility needs. Having no wish-list is neutral (0.75).
  - Shared must-haves count 1, a must-have shared with a nice-to-have 0.75, shared nice-to-haves 0.5.
- **Consent (simulated):** your side is your explicit like. Each made-up person accepts you if no dealbreaker applies and the score meets their own pickiness.
- **Groups:**
  - Suggestions use only your mutual matches who also accept each other; previews change nothing; "Propose this group" forms it.
  - Groups are re-checked after every profile edit, and the brief is hidden while a group is broken.
  - Suggestions show the best group of each size, ordered by weakest pair, then average.
- **Home brief:** total bedrooms and bathrooms. Size is an editable range of useful area: the estimate ±10%, rounded to 5 m². The estimate is (bedrooms × 12 + bathrooms × 5 + 30 + 12 + desks × 5 + 12 if a second living space is needed + 3 for a guest WC when suggested) × 1.15.
- **Room split:** an illustrative bedroom allocation only. It doesn't determine ownership, payments or use rights.

## Still open
- The weights and the size formula are uncalibrated first guesses; tune them after the pilot.
- The household-type preference remains a demo dealbreaker until D-009.
- The prototype is in English; user tests need a Spanish version (B-018).
- Which questions predict long-term compatibility between co-owners, as opposed to flatmates?
- What's the minimum shown before identities are revealed? How do we verify people?
- How do we weigh money compatibility (budgets, deposits, appetite for risk) lawfully?
