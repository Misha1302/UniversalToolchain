# Documentation reset 2026-07-04

This reset separates current truth from historical context and future proposals.

## What changed

- Removed generated `docs/index.html`; VitePress source remains `docs/index.md`.
- Moved vision notes to `docs/archive/vision/`.
- Moved the broad project overview snapshot to `docs/archive/reviews/`.
- Moved the typed module contracts design to `docs/proposals/`.
- Moved the Flame SSA optimizing backend design to `docs/proposals/`.
- Left redirect-style Markdown pages at old locations to avoid hard link breaks.
- Added documentation-status rules and a lightweight documentation fitness check.

## Safety model

This is intentionally conservative. It does not delete historical source
material; it changes its authority level.

Current-state documents are allowed to govern implementation. Archive and
proposal documents are allowed to inform design, but they must not be treated as
implemented behavior unless current-state docs and tests agree.

## Validation

Run these checks after documentation edits:

```text
npm run docs:status
python3 Tools/check_documentation_status.py
python3 .github/scripts/run-markdown-bash-blocks.py
npm run docs:build
```
