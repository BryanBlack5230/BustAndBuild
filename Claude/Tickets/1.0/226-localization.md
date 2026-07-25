---
id: 226
title: Localization
area: Ship
status: open
assignee:
blocked-by: [218]
---

## What to build

Localization prep (TaskBreakdown 13.5) — a 1.0 ship-line item. Externalize **all** player-facing strings (UI, panels, onboarding one-liners, tooltips) into tables; language option lands in Options; at least one non-English locale wired end-to-end as proof the pipeline works. Gibberish stays gibberish — the fiction (deity can't understand them) means zero VO to localize. Runs after onboarding (218) so the string surface is stable when it's externalized.

## Acceptance criteria

- [ ] No hard-coded player-facing strings; everything table-driven
- [ ] Language dropdown in Options; one non-English locale fully playable
- [ ] Font/glyph coverage for target locales; UI layouts survive longer strings
- [ ] Adding a new locale is a data task, not a code task

## Blocked by

- Onboarding (218)
