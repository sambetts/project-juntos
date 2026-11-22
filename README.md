# project-juntos

Private codename for a new company, run mostly by AI agents with Sam Betts as founder. The idea: **help compatible people in Spain buy one home together**, matched dating-style, then help with the joint home brief, the search and, later, the legal set-up and exits.

- **Status:** Phase 0 (explore and set up), in Plan mode. Nothing public, nothing bought, €0 spent. Done so far: research, a playable prototype, an independent review, a map of rivals, a lawyer brief and a pilot plan. Instructions tuned for Opus and a kickoff guide added on 2026-11-20. On 2026-11-21 Sam checked his employer's side-project policy and moved the project to his personal resources (D-021); on 2026-11-22 it became this private repository (D-022). Last updated 2026-11-22.
- **Employer rules:** Sam is employed full time. His employer's side-project policy allows this project outside work devices, accounts, systems and working time, so it lives on his personal resources (F-001, D-021); reading his employment agreement is the last step of F-001. AI sessions stay in Plan mode (research, plans and drafts only; nothing registered or published) until Sam writes a dated switch to Build mode (AGENTS.md section 3).
- **Name:** not chosen. Provisional favourite **Compicasa**, pending a proper clearance search (`research/2026-11-20-naming-screen.md`). "project-juntos" is only this repository's codename: "Juntos" itself isn't available, as its .es and .com domains are taken.
- **Canonical copy:** this repository, private on Sam's personal GitHub account (D-022). The copies on his work OneDrive are retired, and Sam deletes them (D-021, F-012). Keep it private: only Sam changes its visibility or collaborators.

## Start here
| You are | Read |
|---|---|
| Sam, starting an AI session | `KICKOFF.md`: the checklist and the prompts to paste |
| Sam, catching up | `STATE.md` (where things stand), then `FOUNDER-QUEUE.md` (what needs you) |
| An AI starting a session | `AGENTS.md`, then what its section 5 lists: STATE, FOUNDER-QUEUE, BACKLOG, DECISIONS, RISKS and the newest LOG entry |

## Repository map
| Path | What's in it |
|---|---|
| `KICKOFF.md` | How Sam starts a session: checklist, prompts and model settings |
| `AGENTS.md` | Rules for every AI: modes, who decides what, how a session runs, how to report |
| `STATE.md` | Current state and operating mode, rewritten every session |
| `LOG.md` | Session log, newest first; old entries are never edited |
| `DECISIONS.md` | Decisions made and pending, with options and recommendations |
| `FOUNDER-QUEUE.md` | What needs Sam: approvals, purchases, calls, sign-ups |
| `BACKLOG.md` | Prioritised work with owner role, status and gates |
| `RISKS.md` | Risk register |
| `.github/agents/` | One file per AI role; GitHub Copilot uses them as custom agents |
| `.github/copilot-instructions.md` | Points GitHub Copilot to `AGENTS.md` |
| `project/` | Charter, roadmap, glossary of Spanish property terms, budget ledger, experiment log, partner tracker, and Sam's role (`project/founder.md`) |
| `research/` | Dated research notes, including the independent review and the lawyer brief |
| `product/` | Profile and matching model, prototype notes, Phase 1 pilot plan |
| `prototype/` | Playable single-file prototype and its checks (run `node test_proto.js` inside `prototype/`) |
| `templates/` | Session hand-off, decision record, experiment card, research brief |

Older log entries and research notes mention `agents/` and the OneDrive folder "Co-buying startup": they're records and stay as written. The role files are now in `.github/agents/`, and Sam's role is in `project/founder.md`.

## Using GitHub Copilot
- The files in `.github/agents/` are custom agents: Markdown with YAML front matter, with a `name` and the required `description`, which says what the agent does and when to use it. Start each session with **chief-of-staff** and a prompt from `KICKOFF.md`, and check the agents appear in the agents menu before relying on them.
- To pin the model, add `model:` with the exact name shown in the model picker (a list sets fallbacks in order). `reasoning-effort:` (low, medium, high, xhigh or max) is optional: start with the default, and raise it for Legal & Compliance or the CEO only if their work looks shallow.
- Consider limiting each agent's tools with `tools:`.
- Other AI tools work too: the prompts in `KICKOFF.md` tell the AI to read `AGENTS.md` first.

Sources, checked 2026-11-20: [VS Code custom agent file reference](https://code.visualstudio.com/docs/agents/reference/custom-agent-file-reference); [GitHub custom agents configuration](https://docs.github.com/en/copilot/reference/custom-agents-configuration).
