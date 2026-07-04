# Architecture proposals

This directory contains design proposals and rollout plans.

Proposal documents are not implemented behavior by default. A proposal becomes
current architecture only after the implementation lands, tests cover it, and
the current-state documents are updated.

Required promotion path:

1. Implement through existing architecture boundaries.
2. Add or update fitness tests that protect the new rule or invariant.
3. Update `docs/CURRENT_ARCHITECTURE_STATUS.md`.
4. Link the implemented design from `docs/DOCUMENTATION_INDEX.md`.

