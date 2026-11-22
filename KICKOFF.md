# Kickoff: starting an AI session

For Sam: how to start, or restart, an AI session on this project. Written on 2026-11-20 for the newest Opus model (Opus 5.5 at the time); updated on 2026-11-22 for this private GitHub repository (D-021, D-022).

## Before the first session
| # | What | Why | Status |
|---|---|---|---|
| 1 | **Push to your private repository (F-012)** | Decided on 2026-11-22: the project lives in a private repository, project-juntos, on your personal GitHub account, and runs on your personal AI subscription (D-021, D-022). Done 2026-11-22 (F-012) | Done |
| 2 | **Employer check (F-001)** | Cleared 2026-11-22 (D-023); where the project runs is your choice (D-026) | Done |
| 3 | **Confirm the modes (F-011)** | The rules for what the AI may do on its own (AGENTS.md section 3) | Open |
| 4 | **Time and money (F-002)** | Sets your weekly hours and the spending cap the plans are costed against | Open |
| 5 | **Choose Opus if your tool offers it** | The instructions are tuned for it. Pick the newest Opus in the model picker; if the tool offers an effort setting, leave the default. Another capable model works too: the first-session prompt checks the pack with it | Every session |
| 6 | **Answer what you can** | Write "ANSWER:" under items in FOUNDER-QUEUE.md; the session applies your answers first | Any time |
| 7 | **Build mode, when you want it** | Sessions stay in Plan mode (documents only) until you write "Build mode from <date>" in FOUNDER-QUEUE.md. Build mode allows prototypes and code, but nothing public, paid or sent without you | When ready |

Start sessions only after item 1, so they run on your own tools. Items 2 to 4 can wait: the session stays in Plan mode.

## First session with a new AI or tool: paste this
> You're the Chief of Staff for my co-buying startup project, an AI-run company where I'm the founder. The project is this repository, project-juntos. Read AGENTS.md first and follow it: it says what to read next, what you may do on your own and how a session runs.
>
> As this is your first session, give me your opening brief, then do a quick cold-start check before your goals; it doesn't count as one of them. A full check was done on 2026-11-20, so note only problems that would stop you or lead you to break a rule. Fix what's within your authority (AGENTS.md section 11); put changes that need me in FOUNDER-QUEUE.md and other follow-ups in BACKLOG.md.
>
> Then run a normal session: up to three goals from the top of the backlog that the current mode allows.
>
> Done means: each goal is done or its blocker is written down; LOG, STATE, BACKLOG, DECISIONS, RISKS and FOUNDER-QUEUE are current and the saves checked; and I have your closing report, "Needs you" first.
>
> Keep going without me unless a step needs my decision, is on my list in AGENTS.md section 4, or a tool asks for my approval.

## Every later session: paste this
> Continue my co-buying startup project as its Chief of Staff. The project is this repository; read AGENTS.md first and follow it. Done means: up to three goals, each done, blocked with the blocker written down, or left with a clear next step; the project files current and the saves checked; and your closing report with "Needs you" first. Keep going without me unless a step needs my decision or approval.

## A session on one item: fill in and paste
> Continue my co-buying startup project as its Chief of Staff (this repository; read AGENTS.md first). This session, work only on B-___: <what you want>. Done means: <the output file> exists and does what the backlog row says, the project files are updated and the saves checked, and I have your closing report. Stop and ask only if <condition>, or if a step needs my decision or approval.

In GitHub Copilot, pick the **chief-of-staff** agent before you paste a prompt. If your tool can't change the repository, the session gives you the changed files to commit yourself (AGENTS.md section 10).

## During and after a session
- You can add a note while it works, and it will take it into account. If your tool asks you to approve saves, approve each prompt before sending a new message.
- When it finishes, read **Needs you** first, then check its commit (or review and merge its pull request) and push.
- If it stops to ask whether to carry on and nothing needs you, reply "continue".
- If a session drops, start a new one with the "every later session" prompt: it picks up from "Session in progress" in STATE.md.
- After a new model version or a change of tool, use the first-session prompt again: it includes the cold-start check (B-023).
