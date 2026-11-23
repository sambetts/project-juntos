# Calmer navigation and pages (Session 18, B-051)

Sam asked (2026-11-23): "The matches screen is very busy especially. They all are. Make them all clearer & critique the format generally … restructure the nav & the pages so it's not so overwhelming. The UI should be easier to flow through the process and not have everything presented at once. Example: groups tab has the groups you've formed, and then each in group the group fit (various panels) + homes for sale that fit + the homes themselves. It's too much for a single page." And: "'edit your profile' and 'back to matches' on the bottom seem to take a lot of space."

By Product and the Builder, from full-page screenshots of a local run with made-up data (Playwright driving Edge, 1280 and 390 px), before any change. The plan below was reviewed by a second model (Fable 5.2); what changed because of that review, and what was built, are at the end.

## Summary
- **Diagnosis:** each page does every job of its step at once. Matches is 7,700 px tall on a desktop (four jobs: stats, score explainer, group suggestions, then filters, chart and 36 dense cards). A group's page is 5,000 px on a desktop and 10,000 px on a phone (members, fit, brief, rooms, agreement, filters, five tiles, three charts and 42 homes).
- **Navigation is doubled:** numbered pill tabs at the top, plus a fixed bar at the bottom that mostly repeats them ("Edit your profile", "Back to matches") and costs about 70 px on every screen. On a phone the header wraps to three rows (about 175 px) before any content.
- **Plan:** three plain destinations (People, Groups, Profile); a bottom tab bar on phones and tabs in the header on desktops; no permanent action bar. Each page shows a summary first and splits its jobs into sub-views: People into *To review*, *Liked*, *Matches* and *Passed*; a group into *Overview*, *Fit*, *Home brief*, *Homes* and *Agreement*. Charts stay but open on request. Every view gets its own address, so the back button works.
- **Kept:** all data, rules and wording that matter (made-up tags, the "not advice" notes, D-035's link-only homes, D-037: people are never filtered by household type or money).

## Findings (before)
| # | Screen | Finding |
|---|---|---|
| 1 | All | Two navigations: top tabs and a bottom bar that repeats them. The bar's left button ("Edit your profile") and right button ("Back to matches") are tab switches dressed as actions |
| 2 | All (phone) | Header wraps to three rows: logo and tagline, the proof-of-concept badge, "How it works", the language button. Tabs scroll sideways under it; the third tab is cut off |
| 3 | Matches | Five stat tiles open the page, though the numbers aren't what people came for; on a phone they fill the first screen |
| 4 | Matches | Group suggestions (the next step) sit between the explainer and the list, so people who haven't liked anyone yet see "groups" before people |
| 5 | Matches | Everyone in one grid: new people, people you liked, mutual matches and passes mixed, sorted by score. The status filter fixes this but is one of four controls in a toolbar |
| 6 | Matches | Each card has about 15 parts: avatar, name, household, ring, bio, 4–5 facts, 4–6 chips, "together you'd need", "why", status and two buttons |
| 7 | Groups list | Fine, but invitations and formed groups look the same apart from a small label |
| 8 | Group | Members, fit matrix, the brief, rooms, placement tips, agreement starters, then the homes explorer (filters, five tiles, three charts, cards) on one page. Uneven card heights leave big holes in the three-column grid |
| 9 | Group | "Leave the group" sits after the 42 homes |
| 10 | Homes | Filters open by default on desktops and take a screen; tiles and charts come before the first home |
| 11 | Profile | Eight form cards at once in a masonry-like grid; the account card sits among them |

## Plan (first draft, before the review; see "Review" and "Built" below for what changed)
### Navigation
- Three destinations without numbers, in order of use after onboarding: **People** (Personas), **Groups** (Grupos), **Profile** (Perfil), each with a count badge where it helps (mutual matches waiting to be grouped; pending invitations).
- **Desktop:** the tabs sit in the header row (logo, tabs, help, language): one bar instead of two.
- **Phone:** a fixed bottom tab bar with icons and labels (thumb reach), and a one-row header: logo, a small "Demo" pill, "?" (the guide) and the language code.
- **No permanent action bar.** A contextual tray appears only when it carries a real action: building a group from your matches (members, Preview, Propose), and the onboarding wizard (Back, Next).
- **Addresses:** `#people`, `#people-liked`, `#people-matches`, `#people-passed`, `#groups`, `#group-<id>`, `#group-<id>-homes` and so on. The back button and shared links then work; `#help`, `#terms`, `#privacy` and `#lang=` keep working.
- **Next step** hint at the top of each page, one line with one button ("You have 3 mutual matches: form a group"; "Tom hasn't answered yet"; "12 homes fit: see them").

### People (was Matches)
- Header: title, one-sentence lead, one summary line ("36 compatible · 8 hidden by your dealbreakers · median affinity 79 %") instead of five tiles.
- **Sub-views with counts:** *To review* (not answered yet), *Liked* (waiting or said no), *Matches* (mutual), *Passed*. Opens on *To review* while anyone is left, else *Matches*.
- *Matches* leads with "Groups that could work", then the mutual matches with "Add to group". Group suggestions leave the other views.
- Toolbar: one row (search, minimum affinity, sort). The affinity histogram moves behind "Show the chart"; "How the score works" becomes a short link-style disclosure. "Hidden by your dealbreakers" stays a closed disclosure at the end of *To review*.
- **Compact card:** avatar, name, household, ring; bio clamped to two lines; at most two "why you click" chips and one "talk about"; the actions. "More" opens the facts, "together you'd need", the other chips and the score breakdown.

### Groups
- List: *Invitations* first (with Open, Accept, Decline), then *Your groups*, each with a state pill and the weakest-pair score.
- **Group page:** a compact header (back to all groups, avatars and names, state pill, Accept/Decline if invited), then sub-tabs:
  - **Overview:** three linked summary tiles (how you fit → Fit; the home you'd need → Home brief; homes for sale that fit, with count and median price → Homes) and a "Next step" card. Preview banner and Propose for a preview. "Leave the group" at the end of this view only.
  - **Fit:** the scores, the pair matrix, the weakest pair's breakdown.
  - **Home brief:** headline and key numbers, size editor, where, must-haves, nice-to-haves, worth looking for, and who gets which rooms.
  - **Homes (N):** list first. A compact toolbar (sort, price, bedrooms, "More filters" for the rest); a one-line summary; a *List* / *Market* switch where *Market* holds the stat tiles and the three charts. Cards show at most three fit tags plus "+N".
  - **Agreement:** the house-agreement starters.
- A preview (not proposed yet) has Overview, Fit, Home brief and Agreement; Homes appear once the group is proposed, as today.

### Profile
- Each section is a collapsed row: title, a one-line summary of the current answers, "Edit". Opening one shows its fields; the live "N compatible" count sits in the page header instead of the bottom bar. Examples move behind "Start from an example". Account (sign out, delete) sits apart at the end.

## Rules this keeps
Spanish first and English for every new word (D-024); colours from CSS classes under the strict CSP (no inline styles); people never filtered by household type or money (D-037); homes show facts and the agency's link only (D-035); "made-up" tags and "not advice" notes stay visible where they are now.

## Open questions
- Bottom tab bar on desktop too, for consistency? Plan: no; the header row is shorter.
- Should the *Market* view be the default for a big result set (Sam asked for "analytical" in Session 16)? Plan: list first, with the summary line always visible.

## Review by a second model (Fable 5.2, 2026-11-23)
Verdict: "build with changes". All eight changes taken:
1. **Group page: three views, not five.** Fit and the agreement starters don't need their own tabs; five tabs would scroll sideways at 390 px, the problem finding 2 names. Now *Overview* (with how you fit), *Home brief* (with rooms and agreement starters), *Homes*.
2. **People: three views, and no "said no" list.** Likes are silent (B-050); a list headed "said no" would announce a rejection. Someone who passes quietly leaves *Liked*. *Passed* moves to a link at the end of *To review*.
3. **"More" opens a person sheet** (a dialog; full screen on phones) at `#person-<id>`, so cards stay one height and Back closes it.
4. **Group suggestions move to Groups**; People › Matches gets one "See groups" line. One next-step element per page.
5. **The first card shows on the first phone screen** (no lead paragraph, no tiles, one summary line).
6. **The phone header keeps "made-up people"** in its short badge.
7. **A real empty state for Homes** (made-up homes are off on the live site, F-019, so it will be common): what to change in the brief, and a link to it. In a preview, *Homes* shows greyed out "after you propose" instead of appearing later.
8. **Tabs are links** with `aria-current`; the heading no longer shows a focus outline when focus moves to it by script; the group-building tray replaces the bottom tab bar on phones rather than stacking on it.

Also taken: only invitations get a count badge; the *Market* view keeps Session 16's tiles and charts unchanged.
Not taken: a small inline price chart above the homes list (the summary line carries count, median price and €/m²; the charts are one tap away), and remembering *List*/*Market* per person.

## Built (Session 18)
Front end only (`app/src/Compicasa.Api/wwwroot/`): `index.html`, `app.js`, `app.css`, `i18n.js` (Spanish and English for every new string), `sw.js` (version bumped). The in-app guide (B-050) now uses the new names. Beyond the plan: people show 12 at a time with "Show more", like the homes; a suggestion you already have as a group isn't suggested again; the profile's summary lines say what you answered ("doesn't smoke", "must: storage room").

**Page heights before and after** (full page, made-up data, Playwright with Edge; Verified by screenshot size on 2026-11-23). The "before" phone pages were 482 px wide, not 390: the old header and tabs pushed the page sideways on phones. The new pages are exactly 390 px wide.

| Page | Before, 1280 px | After, 1280 px | Before, phone | After, 390 px |
|---|---|---|---|---|
| Matches → People, *To review* | 7,734 | 1,857 | 19,495 | 4,314 |
| People, *Matches* | (same page) | 1,200 | (same page) | 1,953 |
| Group with 42 homes → *Overview* | 5,047 | 953 | 10,212 | 1,389 |
| … *Home brief* | (same page) | 1,349 | (same page) | 2,304 |
| … *Homes* (12 shown) | (same page) | 2,256 | (same page) | 5,389 |
| Profile | 2,126 | 1,124 | 4,297 | 1,255 |
| Groups list | 900 | 992 | 1,091 | 1,102 |

The Groups list grew slightly because suggested groups moved there from Matches.

**Checks:** `dotnet test` 40 passed; the browser test, now 42 checks (7 new: People's address, no bottom bar without a draft, three People views, the person sheet opening and closing, a group's address, the brief as its own view, Back to the overview; homes are checked in their own view), passed twice against a local run.

## Still open
- The *Homes* cards are still the busiest element (plan sketch, price, €/m² and its change, facts, place, up to three tags, the "check" line, the link). A next pass could fold the €/m² change and the check line into the tags.
- A person's sheet has no Previous/Next, so reviewing many people means opening and closing it each time.
- Phones checked in headless Edge only; a real phone and a screen reader still need a person (B-025).
