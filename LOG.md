# Log

Newest first. Add an entry at the end of every session (`templates/session-handoff.md`). Don't edit old entries; correct them in a new one.

## 2026-11-22 · Session 9: name suggestions
**Who:** Sam with Claude Code (Opus 5.5) on his personal machine, as Chief of Staff, with Brand & Marketing (B-026) and the CEO (D-007 options)
**Mode:** Build (D-023) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 8"; newest LOG entry Session 8
**Goals:** 1. B-026 (new), a fresh name longlist with a quick public screen, because Sam asked: "We need name suggestions".
**Needs Sam:** F-003 updated: take Compicasa and Sumallaves into the clearance search together, then choose.
**Did**
- B-026 (Brand & Marketing): screened 71 new names. For .com, the registry's public lookup (Verisign RDAP); for .es, public DNS name servers only, which is indicative. Web searches for existing use of the top names. Result: `research/2026-11-22-name-longlist.md`, with a top five (Sumallaves, Condueños, Anidamos, Casa Socia, Casa al Alimón), the other unregistered names, and the names ruled out with reasons (for example Fincamos is too close to the registered AFINCA mark; Copropia; Arraigo).
- Updated D-007's options (CEO), F-003, B-024 (now clears Sumallaves too, and confirms the .es addresses), B-019 (adds Viviendea) and STATE.

**Decided:** none; the name is Sam's (F-003). D-007's options now list the new names.
**Approvals used:** none. Nothing was bought, filed, published or sent.
**Learned:** about 20 of the 71 names have neither address registered, but most are descriptive phrases, which are weaker as marks. Spanish agency names that use "llaves" are common. Found Viviendea, a platform that groups housing seekers by interests before approaching a developer (Reported, construible.es 2018); it isn't in B-001. Not checked: trademark registers, whether the .es addresses are really free, and what Anidando S.L. does.
**Open or blocked:** F-003 waits on Sam; B-024 can start now.
**Saved and checked:** reviewed `git diff` for BACKLOG, DECISIONS, FOUNDER-QUEUE and STATE, and re-read the new note, against what I meant to write. LOG.md saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-007 bilingual landing-page draft. 2. B-013 privacy and safety groundwork. 3. B-024 clearance of Compicasa and Sumallaves.

## 2026-11-22 · Session 8: Build mode, bilingual prototype
**Who:** Sam with Claude Code (Opus 5.5) on his personal machine, as Chief of Staff, with the CEO (D-023, D-024), Builder and Tester (B-018)
**Mode:** Plan, then Build from 2026-11-22 (D-023) · **Copy used:** canonical repository (D-022)
**Started from:** STATE.md "Last updated 2026-11-22 after Session 7, continued"; newest LOG entry Session 7, continued
**Goals:** 1. B-018, the Spanish version of the prototype, with the Tester's checks; widened mid-session by Sam to a bilingual version (D-024).
**Needs Sam:** F-012 updated: the push is done; deleting the work copies is all that's left. F-011 updated: it now only confirms the modes table, since Build mode has started. B-025 new: try the prototype on your phone, or approve a Spanish-speaking reviewer.
**Did**
- Opening: Sam asked for the project "to be built". The project was in Plan mode, so I asked whether he meant a Plan-mode session or Build mode. He chose "F-001 cleared, Build from today", confirming he'd read his employment agreement and found no approval, exclusivity or IP clause that blocks the project. Recorded as D-023; F-001 closed; mode and R-008 (score 6 to 3) updated.
- F-012: the repository is pushed (the clone tracks the GitHub remote and has its first commit), so only deleting the work copies is left.
- Cold-start check, first session in Claude Code: nothing stopped me or would have led me to break a rule. AGENTS.md section 10 covers this tool under "a coding assistant working in the repository".
- B-018 (Builder): first translated the prototype into Spanish in place. Sam then asked mid-session for the site to be "dual lingo, english & spanish", with documents kept in English (D-024). Rebuilt it as one bilingual file, `prototype/juntos-co-buying-prototype.html`:
  - every user-facing string sits in an English–Spanish string table, or an `{en, es}` label on the data, including all 14 bios
  - a language switch in the header; the page opens in the browser's language, remembers the choice, and `#lang=en` or `#lang=es` forces one
  - the page's `lang` attribute and title follow the language
  - Spanish uses tú and vosotros, avoids gendered words where it can, and follows the y/e and o/u rules
  - new storage key `juntos-prototype-v3`
  - the matching logic is unchanged: the same compatible people, mutual matches and groups as before, for both example profiles
- B-018 (Tester): rewrote `prototype/test_proto.js`, growing it from 111 to 193 checks.
  - Every screen check runs in English and in Spanish.
  - New checks: both string tables have the same keys; every label and bio exists in both languages; Spanish grammar; no words from the other language on any screen; switching language mid-session keeps the group and redraws the page.
  - Mutation test: words planted on an English screen and on a Spanish screen were both caught.
  - A first version of the leak check had a bug: its `\b` boundaries were saved as control characters, so it could never fail. Found because it passed the old English file; fixed and re-proved.
- Test record (Tester):
  - `node test_proto.js`: 193 passed, 0 failed.
  - Headless Microsoft Edge: loaded `#lang=es` (home), `#lang=en` (matches) and `#lang=es` (profile) with no script errors; the page `lang` and title followed the language; desktop screenshot at 1280 px looked right.
  - A 390 px window rendered wider than the screen, but the English-only version from before this session does the same, so that's how headless Edge lays out narrow windows, not a new bug. Mobile is untested.
  - Not tested: real phones, Firefox and Safari, a screen reader, and a native speaker's review of the AI-written Spanish. All are in B-025.
- Updated: `product/prototype-notes.md`, `product/profile-and-matching-model.md`, the Builder and Tester role files, AGENTS.md section 9 (the product is bilingual: an out-of-date fact, within the Chief of Staff's remit), BACKLOG (B-018 done; B-025 new; B-007, B-012, B-006 and B-011 no longer wait on F-001 or Build mode), STATE, RISKS and FOUNDER-QUEUE.

**Decided:** D-023 F-001 cleared, Build mode from 2026-11-22 (Sam) · D-024 bilingual product, English documents; amends D-014 (Sam; built by the Builder)
**Approvals used:** Sam's choice in Claude Code on 2026-11-22, "F-001 cleared, Build from today", quoted in D-023 and F-001; Sam's message of 2026-11-22, "Keep docs in English; the site should be dual lingo, english & spanish. I work better in English", quoted in D-024. Nothing was published, sent or paid for.
**Learned:** Node now has a `navigator` object, so the tests saw "en-US" and ran in English until they set the language explicitly. Headless Edge can't stand in for a phone. That many co-buyers in Madrid are international residents is an assumption behind D-024, not checked.
**Open or blocked:** B-025 waits on Sam (a phone check, or approval for a Spanish-speaking reviewer). F-012's deletion and every pilot item (F-002 to F-011) remain with Sam.
**Saved and checked:** after saving, re-ran `node test_proto.js` (193 passed) and reviewed `git diff` for every changed document against what I meant to write, which caught two stale lines in F-001 and F-011, now fixed. LOG.md saved last and checked with `git diff`; the commit is confirmed in the closing report.
**Next session should start with:** 1. B-007 bilingual landing-page draft, not published and storing nothing. 2. B-013 privacy and safety groundwork. 3. Sam's remaining answers.
**After the closing report:** Sam answered "F012: done" (F-012 closed) and, on B-025, "I'll worry about it when the time comes" (set to later). He asked what names were on the table (answered from `research/2026-11-20-naming-screen.md`; F-003 is still his), then clarified D-024: "Spanish should be prioritised … just there should be support & some consideration for English speakers." The prototype now always opens in Spanish instead of following the browser's language; 194 checks pass, including one that runs in an English-language (en-GB) environment. Saved and checked with `git diff`; committed.

## 2026-11-22 · Session 7, continued: ready for GitHub as project-juntos
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff), still on Sam's work systems, to package the project for its move
**Mode:** Plan · **Copy used:** canonical folder on the work OneDrive (D-016, D-021), repackaged as the repository
**Started from:** STATE.md and the LOG entry from Session 7
**Goals:** 1. Make the project ready for a private GitHub repository named project-juntos.
**Needs Sam:** F-012 (updated): push to a private repository named project-juntos on your personal GitHub account, then delete the work copies.
**Did**
- Checked the work OneDrive copy first: all 43 files matched the final copy by content fingerprint, and nothing from Sam's day job is in them apart from F-001's summary of his own side-project obligations.
- Made it a repository: the role files moved to `.github/agents/`, where GitHub Copilot uses them as custom agents, and Sam's role to `project/founder.md`. Added `.gitignore` (operating-system files, dependencies, secrets, and `private/` and `participants/` as a safety net for personal data) and `.github/copilot-instructions.md`, which points to AGENTS.md.
- Fixed the front matter of `.github/agents/builder.agent.md`: a colon in the description made it invalid YAML, so GitHub Copilot couldn't have read it. The description is now quoted; its wording is unchanged.
- Updated AGENTS.md sections 3 to 5, 8 and 10 (the repository is canonical, a commit closes each session, nothing personal or secret is committed), README (now the repository's front page), KICKOFF.md (prompts for the repository; start with the chief-of-staff agent), STATE.md, F-012, D-022, R-020, B-007, B-018, the pilot plan, the partner tracker, the roadmap and the session hand-off template.
- Older log entries and research notes still mention `agents/` and the OneDrive folder; they're records, so they stay as written (README says where things moved).

**Decided:** D-022 private repository project-juntos on Sam's personal GitHub account; a codename only (Sam)
**Approvals used:** Sam's chat message of 2026-11-22, quoted in D-022.
**Learned:** In a repository the save check gets simpler: a clean `git status` and the commit replace listing files after each save.
**Open or blocked:** F-012 (push, then delete the work copies) and F-001 (employment agreement). Build mode waits for Sam's dated switch.
**Saved and checked:** packaged as project-juntos.zip with 45 files, each read back from the zip and compared with the build folder; the prototype's checks all pass (`node test_proto.js`). The first commit is Sam's (F-012).
**Next session should start with:** 1. In the repository, the first-session prompt in KICKOFF.md, with the cold-start check (B-023). 2. Confirm F-012 is done and close it, then apply Sam's answers. 3. B-013 privacy and safety groundwork.

## 2026-11-21 · Session 7: employer policy check; wrap-up on work systems
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, with Legal & Compliance for F-001). The last session on Sam's work systems
**Mode:** Plan · **Copy used:** canonical folder on the work OneDrive (D-016), retired by D-021 after this session
**Started from:** STATE.md after Session 6; newest LOG entry Session 6
**Goals:** 1. Finish Session 6's save. 2. Check the employer's side-project rules (F-001). 3. Wrap up for the move to personal resources.
**Needs Sam:** F-012 (new, first): move the folder to your personal storage, then delete the work copies. F-001: read your employment agreement. F-011: confirm the modes, and write "Build mode from <date>" when you want sessions to build.
**Did**
- Session 6's save had paused for approval. It finished on 2026-11-21: all 43 files matched the local copies, and nothing had been edited online.
- F-001: read the employer's side-project and conflict-of-interest policies and its code of conduct, and checked Spanish law (Workers' Statute art. 21; Intellectual Property Act art. 97.4). Summary in F-001. In short: side projects need no sign-off unless the employment agreement says so, but they mustn't use work devices, accounts or systems, or working time.
- Sam decided to move the project to his personal resources (D-021, which makes D-013 and supersedes D-016's location). F-007 closed; F-012 added.
- Made the pack work off work systems: README, KICKOFF.md, STATE.md, AGENTS.md (the reason for Plan mode in section 3, a new off-limits line in section 4, the save-approval note in section 5, and section 10 on where sessions run), R-008 (re-scored) and R-020, and the pilot plan's gates.

**Decided:** D-021 (Sam) · D-013 (Sam, via D-021)
**Approvals used:** Sam's chat message of 2026-11-21, quoted in D-021 and F-001.
**Learned:** Sessions 1 to 7 ran on work systems, which the policy rules out for side projects; from now on sessions run only on Sam's own resources. Sections 4 and 5 of AGENTS.md changed and the tool will change, so the next session runs the cold-start check (B-023), which the first-session prompt already includes.
**Open or blocked:** the move (F-012) and the employment agreement (F-001). Build mode waits for Sam's dated switch.
**Saved and checked:** every file changed in this session, listed in OneDrive after saving, with sizes matching the local copies; LOG.md's own check is confirmed in the closing report to Sam.
**Next session should start with:** on Sam's own tool, after the move: the first-session prompt in KICKOFF.md (with the cold-start check, B-023), then Sam's answers, then B-013.

## 2026-11-20 · Session 6: ready to kick off, instructions tuned for Opus
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, with the CEO for D-014 and D-020); cold-start test by a separate Opus 5.5 session
**Mode:** Plan · **Copy used:** canonical folder (D-016)
**Started from:** STATE.md after Session 5; newest LOG entry Session 5
**Goals:** 1. Check what's missing before a new AI session takes over. 2. Tune the instructions for Opus. 3. Cold-start test (B-017).
**Needs Sam:** F-011 new: confirm the operating modes. Updated: F-001 (clearing it doesn't switch the mode by itself), F-002 (time note), F-003 and F-007 (dates), F-004 (price band wording), F-005 (recommendation, with the corrected M3 rule), F-008 (date and recommendation) and F-010 (it also confirms Sam as the pilot's named responder).
**Did**
- Applied the official prompting guidance for the newest Opus (Opus 5.5, building on Opus 5):
  - every session has a finish line and named stop points: keep going while nothing needs Sam; stop only for his decisions or approvals, or for work outside the mode (`AGENTS.md` section 5)
  - operating modes, Plan, Build and Pilot, say what agents may do right now (section 3, D-020)
  - a fixed closing report with "Needs you" first (section 6)
  - rules on scope, document length, evidence, helpers (subagents) and untrusted web or email text (sections 5, 7 and 8)
  - reasons for rules, calm wording, and no "think carefully" or generic "double-check" lines
- Rewrote the role files: each has a "when to use" description, what to read first and what "done" means, and points to its backlog rows instead of a copied priority list that could drift.
- New `KICKOFF.md`: Sam's pre-kickoff checklist and paste-ready prompts for a first session, later sessions and single items.
- README updated (start here; model settings for GitHub Copilot custom agents). Session hand-off and research-brief templates updated.
- Added B-023 (repeat the cold-start test after a change of model, tool or session rules), B-024 (preliminary name clearance), D-019, D-020, R-021 and F-011; recorded D-014 (Spanish first) as made. B-017 done.
- Cold-start test (B-017): a fresh Opus 5.5 session read the pack with only the first-session prompt, as a dry run. Verdict: it could run a session within the rules without Sam, apart from approving saves. It reported 44 issues (3 high, 13 medium, 28 low), and all but one are fixed:
  - Clearing F-001 no longer reads as switching the mode: only Sam switches, in writing, once F-001 and F-007 are settled. The modes go to Sam to confirm (F-011).
  - New rules for part-done goals, dropped sessions, the log's own save check, ID allocation, and who may change the session rules.
  - The pilot's gates and its M3 decision rule now agree across files, with a new "weak signal" rule; Build-mode backlog items show Build mode as what they wait on.
  - Annex A of the lawyer brief adds pledge names and incident reports. The Finance and Partnerships role files point to the research notes instead of copying their numbers.
  - Not changed: D-016 (which copy is canonical) stays a CEO decision until F-007, as it doesn't widen what agents may do.

**Decided:** D-019 sessions run on the newest Opus (Sam) · D-020 operating modes (CEO, proposed; Sam to confirm in F-011) · D-014 Spanish first (CEO)
**Approvals used:** none.
**Learned:** Opus follows the files literally and reads across them, so many of its findings were places where two files said slightly different things (the mode switch, the pilot gates, the M3 rule). Short finish lines and named stop points do more than long instructions. The dry run took the test session about 18 minutes.
**Open or blocked:** everything beyond Plan mode waits for F-001, F-007 and F-011.
**Saved and checked:** every file changed in this session and the new KICKOFF.md, listed in OneDrive after saving, with sizes matching the local copies; LOG.md's own check is confirmed in the closing report to Sam.
**Next session should start with:** 1. Apply any answers to F-001, F-011, F-002 and F-010. 2. B-013 privacy and safety groundwork. 3. B-022 pilot areas, then B-020.

## 2026-11-20 · Session 5: lawyer brief and pilot plan
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, Legal & Compliance and Validation)
**Copy used:** canonical folder (D-016)
**Goals:** 1. Finish saving Session 4. 2. B-003 lawyer brief. 3. B-002 pilot plan.
**Did**
- **Correction to Session 4:** its save stopped after 12 of its 21 changed files, when the approval prompt was cancelled, so its "Saved and checked" line was premature. The rest, including the new competitor note, were saved in this session.
- **B-003 lawyer brief** (`research/2026-11-20-lawyer-brief.md`):
  - Part A, before the pilot, six questions: our status, personal data, equal treatment, safety and liability, commitment without money, and what we may say.
  - Part B, before any purchase: ten questions, for a quote only.
  - Also: what we think we know, with sources; the data the pilot would collect; a Spanish cover email; a checklist for Sam.
- **B-002 pilot plan** (`product/phase1-validation-plan.md`):
  - About 20 eligible households in one Madrid area and price band; pairs only; run by hand; no money, listings or advice.
  - Commitment without money: two meetings, a joint brief both households approve, an exit-rules worksheet and an optional non-binding pledge.
  - Measures M1–M3 with denominators and decision rules; EXP-001 to EXP-003; safety rules; a data summary.
  - Earliest start Monday 11 January 2027, if the gates clear. About 24 hours of Sam's time. €363 to €1,178.98 including the lawyer (estimate, not approved).
- Updated: BACKLOG (B-002 and B-003 done; new B-020 to B-022), FOUNDER-QUEUE (F-004, F-005 and F-006 updated; new F-010), DECISIONS (D-008, D-009, D-011 and D-012 wording; new pending D-018), RISKS (R-001, R-015 and R-018 updated; new R-020), the charter, roadmap, glossary, budget, experiment log, README and five role files.
- Sam answered "yes" to the next step, taken as both items, while F-001 is still open. Both are planning documents, which F-001's default allows.

**Decided:** none (D-018 added as pending).
**Approvals used:** none.
**Learned:**
- A notary advises free and impartially on a deed they're asked to prepare (Consejo General del Notariado). That helps groups later, not our own legal questions.
- A Madrid guide puts a first consultation with a lawyer at about €60–150 before VAT; specialists cost more.
- Saving to OneDrive can pause for Sam's approval, and sending a new chat message cancels the pending approval.

**Open or blocked:** every gate in section 2 of the pilot plan. The lawyer's answers decide what the profile may collect and show.
**Needs Sam:** F-010 added; F-004, F-005 and F-006 updated.
**Saved and checked:** the files left over from Session 4, the new competitor note and every file changed in this session, listed in OneDrive after saving, with sizes matching the local copies.
**Next session should start with:** 1. F-001. 2. B-013. 3. B-020 and B-022.

## 2026-11-20 · Session 4: rival mapping and prototype fixes
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff, Builder and Tester); Research done by a separate AI agent
**Copy used:** canonical folder (D-016)
**Goals:** 1. B-001 map how rivals actually work. 2. B-015 fix the prototype's consent, copy and contrast issues.
**Did**
- **B-001 rival mapping** (`research/2026-11-20-competitors.md`):
  - In depth: habitacion.com, Yorsio and Casicasa. Briefly: Vivla, Pacaso and 8 other players.
  - On public evidence, nobody lets strangers form a group before choosing a whole home, agree a joint brief and design their own exit.
  - Already common elsewhere: affinity questionnaires, co-ownership shares (pro indiviso) with a notarised pact, and exit services.
- **B-015 prototype fixes** (`prototype/`):
  - *Consent:* groups form only from mutual matches who also accept each other (replies simulated). Previews are read-only, with a new "Propose this group" step. No duplicate adds. Groups are re-checked after profile edits, and the brief is hidden until a broken group is fixed. The best group is chosen by score.
  - *Fairness and privacy:* household type no longer counts in the score. Accessibility needs shape the brief, never the score. Must-haves count more than nice-to-haves. Other people's rejection reasons stay private. Fewer sensitive details in the made-up bios.
  - *Honest copy:* simulated replies and uncalibrated scores are labelled. The room split is labelled illustrative. Size shows as an editable useful-area range. Agreement prompts are clearly not the agreement.
  - *Accessibility:* contrast fixed (brand colour #b3370e; four avatar colours). Tabs are accessible, with arrow keys. Focus is kept when the page redraws, and has a visible outline.
  - *Tests:* they now check results instead of just printing them. 111 checks pass, and the HTML validator passes.
- Sam asked for both pieces of work on 20 Nov while F-001 is still open.

**Decided:** D-017 (working positioning; CEO; reversible; internal only).
**Learned:**
- habitacion.com sells a share with the use of one room, home first, and its buyers use personal loans rather than mortgages (per its founder).
- iAhorro says banks are wary of groups of strangers.
- Casicasa's traction looks thin: 3–5 staff and €5,000 turnover, self-reported.
- Rivals don't publish exact fees.

**Open or blocked:** a Spanish version of the prototype and real-browser checks before any user test (B-018). Casicasa's group flow sits behind its login and wasn't checked.
**Needs Sam:** F-001 to F-009 (unchanged).
**Saved and checked:** every changed file listed in OneDrive after saving, with sizes matching.
**Next session should start with:** F-001, then B-003, B-002 and B-009.

## 2026-11-20 · Session 3b: independent review and fixes
**Who:** Sam with Microsoft 365 Copilot (as Chief of Staff); review by a second AI model (GPT-6.2 Sol, maximum reasoning)
**Did**
- The reviewer read all 35 files, ran the prototype's tests and checked the key facts against sources. Verdict: "Ready for a read-only Phase 0 hand-off, not autonomous operation or real-user recruitment." Full report: `research/2026-11-20-independent-review.md`.
- Corrected facts:
  - A viewing-sheet signature alone doesn't bind a buyer to an agency fee.
  - The 20–25% referral rate is unverified, so it's been removed.
  - OCU found agency fees of 2–6%, averaging 5%, before VAT.
  - Madrid's estate-agent register is voluntary.
  - The wording of Civil Code arts. 404 and 1522 is now precise.
  - Ending a co-ownership is usually taxed as AJD, though IRPF can still apply.
  - Witei offers no *new* Idealista connections.
  - Casafari's plans are added.
- Changed recommendations, each for Sam to confirm:
  - No charging in Phase 1 (D-010).
  - Phase 1 is brief-only, with no portal links (D-015, amending D-004).
  - The first group is defined without an age band (D-008).
  - Pilot measures are based on introductions accepted both ways (D-012).
  - A stricter employer check (F-001).
- Process fixes:
  - Read DECISIONS and RISKS at the start of each session.
  - Edit only the canonical copy (D-016) and check that write-back succeeded.
  - Approvals are specific, and silence approves nothing.
  - Risks re-scored, with seven added.
  - Backlog dependencies fixed.
  - Budget, experiment and partner ledgers added.
- Logged prototype issues for a later session (B-015): two-way consent, duplicate adds, the preview saving state, stale groups, misleading room-split copy, contrast and tabs.
- Checked the changed files were saved to OneDrive.

**Correction to Session 1:** the competitor scan understated overlap. habitacion.com and Casicasa offer affinity-based matching and group home search; Yorsio offers affinity matching and rent-to-own. "Nobody covers matching through to a home" is unproven (B-001).
**Decided:** D-016.
**Needs Sam:** F-001 to F-009.
**Next session should start with:** F-001, then B-001, B-003, B-002 and B-015.

## 2026-11-20 · Session 3: naming and hand-off pack
**Who:** Sam with Microsoft 365 Copilot (as Research, Brand & Marketing, Chief of Staff)
**Did**
- Screened 33 Spanish and English names: 66 web addresses (.es and .com) on GoDaddy, trademarks on TMView (Spain, EU and WIPO registers), and existing businesses by web search. 28 addresses were free; 7 names had both free. See `research/2026-11-20-naming-screen.md`.
- Recommended **Compicasa**: both domains free, no trademark or business found. Not approved yet.
- Ruled out Juntos (domains taken), Afincamos (AFINCA trademarks cover real estate), Dos Llaves and Hacemos Casa (similar property businesses exist), Copiso (registered trademark) and Mi Mitad (crowded).
- Set up the AI operating model and created this folder.

**Decided:** D-006.
**Needs Sam:** F-001 to F-008.
**Next session should start with:** Sam's answers in FOUNDER-QUEUE.md, then B-001, B-003 and B-002.

## 2026-11-18 · Session 2: where to find homes
**Who:** Sam with Microsoft 365 Copilot (as Research, Legal & Compliance, Partnerships)
**Did**
- Researched how to source listings legally: Idealista's terms and Search API, Casafari, Kyero-format agency feeds, Witei, aggregators; EU and Spanish case law on scraping and linking; photo rights; the data protection authority's view on scraping; agency fee economics; the CNMC fee-fixing case. See `research/2026-11-18-homes-sourcing.md`.
- Recommended a staged plan: links pasted by members first, then licensed data, then agency feeds and referral deals.

**Decided:** D-004.

## 2026-11-18 · Session 1: idea, Spain focus, profile model, prototype
**Who:** Sam with Microsoft 365 Copilot
**Did**
- Competitor scan (Spain and Europe): the closest are room-level players in Spain (habitacion.com, Yorsio, Casicasa); adjacent are fractional second-home models (Vivla, Pacaso). Nobody covers matching, home and exit end to end.
- Captured Sam's profile model and added design principles: groups, an anchor buyer, the profile as a draft agreement, a tie-break rule, finance as a hard filter.
- Narrowed to Spain and Spanish law; outlined legal structures, financing, exit taxes, platform rules and questions for a lawyer.
- Built a playable prototype of the social side. See `prototype/`.

**Decided:** D-001, D-002, D-003, D-005.
