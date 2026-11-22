# Prototype notes: "Juntos" (built 2026-11-18, fixed 2026-11-20, bilingual 2026-11-22)

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
- 14 made-up Madrid profiles, each with an English and a Spanish bio.
- **Languages (D-024):** Spanish first, English supported, with a switch in the header. It always opens in Spanish, whatever the browser's language, and remembers a viewer who switches to English; the page's `lang` attribute and title follow it. Spanish addresses the user as tú and the group as vosotros, and avoids gendered words where it can ("Os gusta madrugar", "Quién fuma: …").
- The Spanish was written by an AI and hasn't been reviewed by a native speaker yet (B-025).
- State, including the language, is saved in the browser (localStorage key `juntos-prototype-v3`; the old English-only key is ignored). Nothing leaves the browser.

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
  - `lang=en` or `lang=es`: choose the language

  For example: `#preset=room&like=all&group=best&tab=home`.
- **Wording:** every user-facing string is in `STR.en` and `STR.es` (inside the logic block), or an `{en, es}` label on the data (areas, features, bios and so on). `t(key, …)` and `tx(label)` pick the current language. Add new copy to both.
- **Test object:** `window.__juntos` exposes the state, the language and the actions (like, addToGroup, proposeGroup, previewGroup, go, lists, setLang).
- **Tests:** `prototype/test_proto.js` (Node). Run `node test_proto.js` inside `prototype/`; the exit code is 1 if anything fails. 194 checks:
  - scoring fairness: symmetry, household type not scored, accessibility needs neutral, must vs nice
  - consent and groups
  - the home brief
  - colour contrast in light and dark mode
  - tab roles
  - copy, and both languages: matching string tables, every label and bio in both, Spanish grammar (y/e, o/u, "Laura y tú"), no words from the other language on any screen, switching language mid-session, and opening in Spanish even in an English-language browser
  - smoke checks of every screen in a simulated page, in English and in Spanish

  Checked in headless Microsoft Edge on 2026-11-22: both languages load with no script errors, and the page `lang` and title follow the language. Not yet covered: phones, a screen reader, other browsers, and a native speaker's review of the Spanish (B-025).
- **When changing it:** no inline event handlers, no `javascript:` links, no external resources. Keep the dark-mode styles, the page language attribute, both languages and 4.5:1 contrast.

## Ideas parked
- A "Homes" tab where the group adds homes and sees how each fits its brief (B-014; waits for D-015 and Phase 2).
- Tune the questions and weights after the pilot.

## Landing-page draft (B-007, 2026-11-22)
- **File:** `prototype/compicasa-landing-draft.html`: one file, no external resources, marked `noindex`. Not published (Sam decides).
- **What it shows:** a draft banner; the hero line ("Si comprar casa por tu cuenta no te alcanza, hazlo en compañía"); a made-up example of a shared brief; four steps (who with, compatible people, the shared brief and exit talk, buying with a lawyer and notary); "what we are not" (no estate agency, no lending, no legal, tax or financial advice, not flat-sharing); six questions and answers; and a waitlist form.
- **The waitlist stores and sends nothing:** the form has no action or method, and no field has a `name`, so nothing can be submitted even without JavaScript. The script validates the email and consent, shows a draft thank-you, and keeps no value. No browser storage, not even for the language.
- **Fields:** email; area and timing (both optional, from fixed lists); consent. Nothing on money, household or protected characteristics (D-009). The privacy notice is marked as still to be written (B-013).
- **Name:** the provisional name (D-007) is the `BRAND` constant, used everywhere through `{brand}`; the static `<title>` is the only other copy. Renaming takes two edits.
- **Languages:** Spanish by default; English behind the header switch; `#lang=en` or `#lang=es` forces one. All copy is in `STR.es` and `STR.en`.
- **Test object:** `window.__landing` (lang, state, setLang, submit, checkForm).
- **Tests:** `node test_landing.js` in `prototype/`, 135 checks: nothing can be sent or stored; the name in one place; both languages complete and used; Spanish grammar (tú and vosotros, y/e, o/u, ¿ and ¡); no promises about money, mortgages or legal outcomes; minimal fields; email validation; contrast in light and dark mode; every screen in both languages (labels, errors and focus, the thank-you, no email kept, switching language). Seven planted faults were all caught.
- **Checked in headless Edge on 2026-11-22:** both languages load with no script errors; the title and `lang` follow the language. Not covered: phones, a screen reader, and a native speaker's review of the Spanish (B-025).

