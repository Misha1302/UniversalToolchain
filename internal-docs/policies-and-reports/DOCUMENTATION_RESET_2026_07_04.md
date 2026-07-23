# Documentation reset 2026-07-04

This reset separates current truth from historical context and future proposals.

## What changed

- Removed generated `docs/index.html`; VitePress source remains `docs/index.md`.
- Moved vision notes to `docs/archive/vision/`.
- Moved the broad project overview snapshot to `docs/archive/reviews/`.
- Moved the typed module contracts design to `docs/proposals/`.
- Moved the Flame SSA optimizing backend design to `docs/proposals/`.
- Left redirect-style Markdown pages at old locations to avoid hard link breaks.
- Added documentation-status rules and a lightweight documentation fitness check implemented by `npm run docs:status` / `Tools/check_documentation_status.py`.

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
python3 .github/scripts/run-markdown-bash-blocks.py
npm run docs:build
```

`npm run docs:status` delegates to `Tools/check_documentation_status.py`; running both commands separately is unnecessary.
