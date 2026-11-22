# Prototype notes: "Juntos" (built 2026-11-18, fixed 2026-11-20)

- **File:** `prototype/juntos-co-buying-prototype.html`: one file, about 64 KB, with no external resources. Offline behaviour still needs checking on real browsers (B-018).
- **Name:** "Juntos" is a placeholder and isn't available (juntos.es and juntos.com are taken). Rename it after D-007 (B-012).
- **Status:** a demo with made-up people and simulated replies. The independent review's fixes are done (B-015, 20 Nov).

## What it does
- **Your profile:** two ready-made examples, "family" and "room".
- **Matches:**
  - Compatible people, with reasons and a demo score.
  - You like people; their replies are simulated from their own settings.
  - Group suggestions come only from mutual matches who accept each other.
  - You can preview a group (read-only) or propose it.
- **Your home together:** a draft brief for the group:
  - bedrooms, bathrooms and an editable useful-area range
  - must-haves and things worth looking for
  - an illustrative room allocation and placement tips
  - agreement prompts
  - A broken group shows what's wrong instead of a brief.
- 14 made-up Madrid profiles. The page language is English (`lang="en"`); make a Spanish version before any user test (B-018).
- State is saved in the browser (localStorage key `juntos-prototype-v2`). Nothing leaves the browser.

## For builders
- **Code layout:** the logic sits between `/* ===== LOGIC START ===== */` and `/* ===== LOGIC END ===== */`:
  - PROFILES, PRESETS
  - hard, homeScore, score, explain
  - accepts, likesBack, mutual, othersAccept
  - groupCheck, groupStatus, groupScore, rankGroups, bestGroups, topGroup
  - houseBrief, roomPlan, placementTips, agreementTopics

  The views follow: viewYou, personCard, groupCard, viewMatches, matrix, viewHome. Then render, focus handling and the event handlers.
- **Link hooks for demos and tests:**
  - `#preset=room`: load an example
  - `like=all`: like everyone compatible
  - `group=best`: form the top suggested group
  - `tab=home`: open a tab

  For example: `#preset=room&like=all&group=best&tab=home`.
- **Test object:** `window.__juntos` exposes the state and the actions (like, addToGroup, proposeGroup, previewGroup, go, lists).
- **Tests:** `prototype/test_proto.js` (Node). Run `node test_proto.js` inside `prototype/`; the exit code is 1 if anything fails. 111 checks:
  - scoring fairness: symmetry, household type not scored, accessibility needs neutral, must vs nice
  - consent and groups
  - the home brief
  - colour contrast in light and dark mode
  - tab roles
  - copy
  - smoke checks of every screen in a simulated page

  The checks don't cover a real browser or a screen reader (B-018).
- **When changing it:** no inline event handlers, no `javascript:` links, no external resources. Keep the dark-mode styles, the page language attribute and 4.5:1 contrast.

## Ideas parked
- A "Homes" tab where the group adds homes and sees how each fits its brief (B-014; waits for D-015 and Phase 2).
- Tune the questions and weights after the pilot.
