# Copilot / agent instructions

Read **`AGENTS.md`** at the repository root first — it contains the orientation and the
file-ownership map (notably: the entire Windows UI lives in `Program.cs`).

Then read **`ARCHITECTURE.md`** before changing anything (token APIs, build/ship commands,
verification traps), and `PLAN.md` for the current status — **§10 is the current work**
(P7 Settings overhaul, P8 inventory & recipes, P9 order-PC images, P10 ship).

Before writing any UI code, read **`DESIGN.md` → "Implementation contract — building a new
screen" (§C0–C8)**. It is the mandatory recipe book: every colour comes from `Skin.*` (Windows)
or `App.xaml` keys (Android) via its intent→role table; geometry (app bar 72, page head 88,
rail 236, radii 10/12/14/18, gaps 8/12/18/20), typography (Segoe UI; 17 pt title, 10 pt body,
9 pt dim), and component recipes (card, pill button, toggle, nav pill, page head, app bar,
dialog, table row, empty state, KPI chip) are fixed numbers to copy, not to reinvent.
ASCII blueprints for the Settings hub, Stock console and Recipe editor are in §C6; run the
10-point anti-drift self-check (§C7) before calling a screen done.

Non-negotiables: never bypass the licence gate; never overwrite `client database\` in
`publish-win10-x64`, `publish-win7` or `tester-release\Windows`; never hardcode a colour outside
`Skin` (Windows) / `App.xaml` + `Theme.cs` (Android); never let a High-tier effect run on Lite;
every change must compile for `net10.0-windows` **and** `net48` **and** build the Android APK;
every change reaches all four variants (Win10, Win7, tester, Android); never commit or push
unless the user explicitly asks.
