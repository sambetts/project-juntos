# Where to find homes: sourcing listings legally (2026-11-18)

**Status:** corrected on 20 Nov after the independent review, which checked the sources. Links are in `research/2026-11-20-independent-review.md`. The original recommendation was adopted as D-004; an amendment is proposed in D-015.

## Recommendation (staged)
| Stage | How | Why |
|---|---|---|
| Phase 1 (proposed D-015) | Brief only: no listing links. Groups agree what their home needs | Links pasted by members lower the extraction risk but aren't permission; Idealista's terms make no exception for them |
| Next | Licensed data: Casafari API; Idealista Search API if approved. Confirm display and deep-link rights in writing | Licensed, with permission |
| Then | 5–10 Madrid agencies send feeds (Kyero XML) in exchange for a referral or per-lead fee | We bring them ready buyers |
| Later | Private sellers list directly; a marketplace for reselling shares | |

## Portals
**Idealista terms** (English version "last updated 12 November 2026"; the Spanish version is binding):
- Banned without express written permission: accessing, monitoring or copying content by robots, spiders, scrapers or other automatic means, or by manual process; bypassing robot exclusions or access measures; linking to any content; mirroring.
- Section 8.1 bans deep links, scraping and copying for commercial or competitive use, including AI training.
- Section 11 of the English version (section 12 of the binding Spanish one) requires written permission before deep-linking or framing. The request needs name, email, phone, company, the page where the link will appear and details of the link.
- Idealista says it only connects users, takes no part in transactions and isn't an estate agency.
- It doesn't accept listings of jointly owned or timeshare property without express authorisation and a specific agreement. This matters for reselling shares later.
- It lists an "API service" among its additional services.

**Idealista Search API** (developers.idealista.com, access request):
- By approval only: apply with a project description; documentation comes after approval.
- At most 50 results per page (a community client's docs); about 100 requests a month (community reports). The current limits, price and display or deep-link rights are unverified.
- Unofficial "Idealista APIs" on RapidAPI advertise bypassing bot defences. Avoid them: ReefAPI says it isn't official and has no agreement with Idealista.

**Fotocasa:** no public search API found; a forum (Burbuja.info) reported refusals. Unverified.

## Data providers and feeds
- **Casafari** (developers.casafari.com): one record per home, merged from portals and agency websites. It can resolve a portal or agency link to its home, and has a REST API and an MCP server. Usage is counted in units: a search is 50, opening a property 10, resolving a link 25. At €0.04 a unit, an illustrative group (10 searches, 30 homes opened, 10 links resolved = 1,050 units) costs about €42 before VAT. Plans: €49 for 1,500 units or €149 for 5,000 a month, plus a 1,000-unit trial. The platform terms (§11.4) allow display in your own app, subject to the restrictions in §11.3; confirm our use, upstream rights and MCP availability (docs and terms differ).
- **Kyero V3 XML** (help.kyero.com, v3.9.1, January 2026): one feed URL per agent; a full feed, not incremental.
- **Witei** agency software (faq.witei.com): exports Kyero V3 XML hourly "for portals, websites, AI tools", with a generic XML gateway for portals not on its list. The agency must contract with the portal first, even a free one. Feed users must copy images to their own servers, not hotlink. No *new* Idealista connections. Its list includes Fotocasa Pro (fotocasa, habitaclia, milanuncios), pisos.com and others.
- **Trovit, Mitula, Nestoria** (Lifull Connect help pages, for non-Spanish markets): sites submit a feed URL that's read periodically; the model is redirecting traffic through affiliation (1001portales). Foreign help pages don't prove the Spanish terms.

## Law
- **CV-Online Latvia v Melons** (EU Court of Justice, C-762/19, 3 June 2021): a specialised search engine that copies and indexes a substantial part of a freely accessible database is extracting and re-using it. The owner may prohibit that if it puts recovering their investment at risk.
- **Svensson** (EU Court of Justice, C-466/12, 13 February 2014): linking to authorised, freely accessible works isn't communication to a new public, unless the link gets round access restrictions. It's a copyright ruling, not permission under a portal's contract terms.
- **Ryanair v Atrápalo** (Spanish Supreme Court 572/2012, 9 October 2012): Atrápalo won. There was no protected database (Ryanair generated its own data) and, on those facts, no unfair competition. The database right protects investment in obtaining, verifying or presenting data. The absence of technical barriers isn't a general licence to take data.
- **Photos:** art. 128 of the Spanish intellectual property law protects "mere photographs" for 25 years; a Madrid appeal court (13 January 2023) rejected a "no originality" defence.
- **Personal data:** the AEPD (criterion of 25 November 2025) treats scraping that collects personal data as processing; that case was closed for lack of proof. Data being public doesn't by itself exempt you from informing people under GDPR art. 14, though art. 14(5) has exceptions.
- **Competition:** the CNMC (case S/0003/20, decided 25 November 2021, announced 9 December) fined Anaconda, Idealista, Inmovilla, Look & Find, MLS, RE/MAX and Witei €1.25M in total (Idealista €730k) for a cartel. The network imposed a 4% minimum commission on sales, one month on rentals and 50/50 splits. The appeal outcome isn't established here.

## Agency economics
- Fees are free and negotiable. OCU (28 January 2026; 60 agencies in five cities) found an average of 5%, in a range of 2–6%, generally quoted before 21% VAT. Trovimap reports about 5.1% for Madrid. The seller usually pays.
- Some agencies try to charge buyers too. A signature on a viewing sheet alone doesn't bind the buyer: what matters is a clear prior agreement, the services given and unfair-terms rules (El País with Legálitas, March 2026). *Corrected: our first note wrongly said such a signature makes the fee binding.*
- Co-agency splits such as 50/50, 60/40 or 70/30 occur (Fotocasa's professional blog). Referral rates in Spain are **unverified**: find out in agency conversations.
- Worked example (computed): a €300,000 home at 5% means a €15,000 fee before VAT (€18,150 with VAT); a 50/50 split is €7,500. That's arithmetic, not a revenue forecast.
- Agree fees agency by agency and never publish a common scale. Ask partner agencies to waive buyer fees for our groups.
