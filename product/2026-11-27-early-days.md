# Early days: few people at launch (v1.0, 2026-11-27)
_(Product, Session 38, for B-096. Sam, 2026-11-27: "what do we do with the fact there'll be very few users on there when we launch? How can we make it less obvious?", then "Make this happen. It's important we get it right". Decision: D-059. Builds: B-097, B-075.)_

## Summary
- **The rule:** we never make the app look busier than it is. No made-up people shown to the public, no padded counts, no "join thousands". Inventing data is off-limits (AGENTS.md section 4). Fake or padded profiles would very likely also be a misleading practice under Spanish consumer law (Assumption: not checked against the Ley de Competencia Desleal this session), and dating apps have faced regulators over fake profiles (Reported from memory, not opened this session: the US FTC's 2019 case against Match Group).
- **What we hide is the headline, not the facts:** while someone has few compatible people, we don't lead with "1 compatible". We say plainly that it's early in their area, show whoever is there, and promise to tell them when someone compatible joins. Then we keep the promise.
- **Make waiting worth it:** the person's own profile, brief and the house browser work alone; people who already know their co-buyers can use everything without matching (B-075).
- **Concentrate people in time and place:** one focus area for promotion (Madrid, to live in: strategy section 4) and, new here, a dated launch window rather than a slow trickle, so people arrive close together.
- **Rounds dropped:** fixed matching rounds suit the hand-run pilot, not an open app. Making someone wait for a date when a compatible person is already there makes a thin pool feel thinner.

## Why it matters
Every matcher abroad hit the empty room: Kohab closed, Husmates matched two groups (strategy section 2, Reported). A newcomer who sees nobody leaves and doesn't come back, and then the next newcomer sees nobody either. The two ways out are more people at once (time and place) and value that doesn't need other people (single-player and friends-first). Pretending is not a way out: one discovered fake profile ends trust in a product whose whole job is trust between strangers.

## The design
| # | Piece | What the person sees | Status |
|---|---|---|---|
| 1 | **Made-up people off for the public** | A new setting, `People:Demo` (on unless "false"). Off: real users see only real people; made-up people, their simulated likes and scripted replies disappear from People, suggestions and counts. On (today's closed preview): unchanged. Switch it off when the invite code goes (release checklist item 11) | B-097, this session |
| 2 | **Early-days card** | When fewer than 5 real compatible people are left to meet (setting `People:EarlyBelow`, default 5; an Assumption to tune from the funnel), People and Home show a card instead of the "N compatible · median" line: «Estamos empezando en tu zona» / "It's early days where you're looking", "there are still few of us, and you're one of the first. We'll tell you as soon as someone compatible with you joins." It lists three things to do meanwhile: finish the profile, look at homes, and (with B-075) bring people you know. Whoever is there is shown as normal below it. No number is given until there are enough to be worth saying | B-097, this session |
| 3 | **"Someone compatible joined" notice** | When a real person finishes the sign-up wizard, every real person compatible with them both ways gets a notice in the bell: «Se ha unido alguien compatible contigo: Ana» linking to their card. At most three such unread notices at once, so a busy day doesn't flood anyone. By email once B-037 has emails, which is where this earns most | B-097, this session |
| 4 | **Text that assumes made-up people** | "New made-up people arrive each morning" and "made-up people sometimes like you first" become true either way: with made-up people off, they say new people join over time and we'll let you know | B-097, this session |
| 5 | **Bring your own co-buyers** | A group of people who already know each other: one person starts a group and shares a private link; whoever opens it signs up (terms, 18+, code of conduct, and the invite code while it exists), finishes the profile and joins the group without a mutual match. The brief, shortlist, voting and costs then work for them. The link expires after 14 days, works for up to 3 people, can be withdrawn, and shows nothing about the group until the person has signed in | B-075, next |
| 6 | **Useful alone** | A public cost calculator (B-076), and the house browser and home brief for one person | B-076; exists in part |
| 7 | **A launch window** | Promote in one burst, in one place: Madrid, to live in, over two or three weeks, rather than a post here and there. People who join close together meet each other while they're still keen. The channels and wording are Sam's (strategy section 4, Stage 2) | F-052 (Sam) |
| 8 | **What we say in public** | No counts of users, no "thousands", no testimonials we don't have. Honest framing that makes small a feature: «Estamos empezando», a free tool, read by a person. The rest of the preview's wording (ribbon, footnote, sign-in lead, legal pages: "everyone here is made up") changes at release, with Sam's approval | B-098 |

## Rules for the early-days state
- It is computed per person from real people only, so it's true for each viewer: someone in Madrid may be past it while someone in Soria isn't.
- "Compatible" means the same hard rules as People (purpose, area, home type, the living-together dealbreakers), counted both ways. People the viewer passed on don't count; people they liked do.
- With made-up people on, the card still shows when real people are few, and says the made-up people below are there to try it out. That keeps the closed preview honest too.
- Household type and budget never enter it (D-009, D-044): the count uses only the rules People already uses for real people.
- The notice names the newcomer only to people who would see them in People anyway, so it reveals nothing new.

## What we measure
`#funnel` already counts finished profiles, mutual matches within 14 days and groups. The early-days threshold is a guess: after the first month, set it from the share of people with a mutual match within 14 days (strategy section 4).

## Rejected
- **Seeding with made-up or "sample" profiles in the public app:** deceptive, even if labelled, once people expect to meet real co-buyers; and our own rules forbid inventing data.
- **Fixed matching rounds:** see the summary. Kept as an option if Sam ever runs a hand-run cohort again.
- **Showing the exact count from day one** (strategy section 4 said "show newcomers honestly how many compatible people are near them"): honest, but "1 compatible" reads as "dead" and drives people away before a second person arrives. The early-days card is just as honest without the number. D-059 amends that line.
- **Widening matches silently** (other areas, looser rules) when the pool is thin: people would meet co-buyers who don't fit. Later, ask the person instead ("would you also consider…?"): proposed as B-099.

## Open questions
- Is 5 the right threshold? (Assumption; tune from the funnel.)
- Does the notice need a "don't tell me about new people" switch before emails exist? Not while it's only in the bell; yes once it's emailed (B-037).
