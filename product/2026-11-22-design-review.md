# Design review of the PoC (Session 16)

Sam asked (2026-11-22): "Run a design review. I want this website to look amazing, inspiring, and analytical (meaning like it has lots of data; reactive to filters and requirements)." By the Product and Builder roles, from screenshots of every screen at phone and desktop size, in light and dark, against a local run with made-up data.

## Summary
- **Before:** the PoC was tidy and honest but read like a form: long lists of cards, few numbers, nothing to filter, and the homes list was a fixed top 24 with no way to explore it.
- **Built this session:** a homes explorer (price, size, bedrooms, type, area, energy and must-have filters; five sort orders; live stat tiles; three charts that redraw as filters change; an illustrative floor-plan sketch on every home), a matches explorer (stat tiles, an affinity histogram you can tap, status and score filters, search), a house-shaped logo, a hero band with headline numbers, and loading placeholders. Spanish and English throughout.
- **Kept within the rules:** no photos or advert text (D-035): the sketches are drawn from the facts only and say they aren't the real plan. People can't be filtered by household type or money (AGENTS.md section 4): the matches filters are status, affinity score and name only. Price filters apply to homes only, in the browser, and aren't stored (Assumption: this doesn't need the lawyer, unlike a stored price band for the group, B-043).
- **Biggest gaps left:** a map, a front page for visitors (B-035), side-by-side comparison of homes, and real-world data behind the charts (only 90 made-up homes locally; the live index has real Tecnocasa homes).

## Findings and what changed
| # | Finding (before) | Change | Status |
|---|---|---|---|
| 1 | Homes: a fixed list of 24, no filters, no sense of the market | API returns up to 200 homes that fit; filters, sort and "show 12 more" in the browser | Done |
| 2 | No numbers at a glance | Stat tiles: homes shown, median price, median €/m² (with its change against all that fit), median size, best fit | Done |
| 3 | Nothing visual for homes (no photos allowed) | Floor-plan sketch per home from bedrooms, bathrooms, garden, terrace and pool; official-style energy badge; small fit ring | Done |
| 4 | Price alone says little | €/m² on each home with "−34 % per m² vs median"; price histogram (tap a bar to filter); price against size scatter with the median €/m² line (tap a dot to jump to the home); median €/m² by area | Done |
| 5 | Matches: a long grid, no way to narrow it | Stat tiles; affinity histogram (tap a bar to set the minimum); filters for status (with counts), minimum affinity and name | Done |
| 6 | The logo was a letter "C" | A house with two doors (two households), in the brand colour, both themes | Done |
| 7 | Every page started with plain text | Hero band with a soft gradient and the page's headline numbers | Done on matches |
| 8 | Lists flashed empty while loading | Shimmer placeholders for homes (still for reduced motion) | Done |
| 9 | Two quick likes of the same person raised a server error (B-045 item 5); it failed the browser test | Like and pass buttons disable on click; the server updates instead of failing | Done |
| 10 | Group page: the brief is strong but dense | Make the group page a dashboard: one hero row (members, weakest pair, homes that fit, median price), then the brief | Proposed |
| 11 | No map | A map of homes once B-043 brings real geography; needs a tile provider, which the strict CSP and cost rules make a decision | Proposed |
| 12 | Can't compare homes | Pin up to three homes and compare them side by side (facts, €/m², fit) | Proposed |
| 13 | Profile changes feel blind | Show "you now have N compatible (+3)" live as sliders move | Proposed |
| 14 | On a 390 px headless window, the action bar's second button runs off the edge (pre-existing) | Check on a real phone first; headless Edge lays out phones wider than the screen (B-025) | Proposed |

## How the charts were made
Hand-written SVG, drawn at the container's real width so labels stay readable on phones, coloured from CSS classes (the page's security policy blocks inline styles). One accent (the brand colour) for "with your filters" over a grey context series, a legend on every two-series chart, hairline gridlines, 4px rounded bar ends, a tooltip on every mark, and the cards themselves as the table view. Checked in light and dark at 1280 and 390 px wide.

## Open questions
- Is a price filter on homes fine before the lawyer answers A3? Assumption: yes, since it filters listings, not people, and isn't stored. Worth a line under A3 in the lawyer brief; not added this session.
- Whether the median €/m² comparisons should name their base ("homes that fit your group"), once real homes outnumber made-up ones.
