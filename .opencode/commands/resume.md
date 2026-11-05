---
description: Resume the Vectron POS work exactly where the last session stopped
---

Resume the Vectron POS work (PLAN.md G-series graphics overhaul) from where the last session
stopped — the daily tokens reset, so assume you have NO memory of previous chats.

Do this in order:

1. Read `RESUME.md` — its **Current state** block names the current phase, the exact next
   action, and the acceptance gate. That block is authoritative.
2. Read `PLAN.md` §9 (status/history) so you do NOT redo completed work, then §3 (decisions
   D1–D8, palettes, animation catalogue, rules) and §4 (the G0–G9 phases).
3. Read `APP.md` for the product brief (what we build, platforms, build commands, NEVER-list).
4. Continue at the phase named in RESUME.md. Work phase by phase; satisfy each ✅ acceptance
   gate before starting the next. Per the user's standing rule, every UI phase must reach
   **Win10 + Win7 + Android** before the next one starts.
5. As you go: after every meaningful step and at the end of the session, update
   **RESUME.md → Current state** AND append to **PLAN.md §9** — that is how the next session
   resumes. Keep RESUME.md ≤ 70 lines.

Hard rules while working: never commit/push unless the user asks · never touch
`client database\` in `publish-win10-x64\` / `publish-win7\` · never kill a running POS
without asking · no license bypass · both TFMs must compile.

Current uncommitted tree (normal for this repo):

!`git status --short`
