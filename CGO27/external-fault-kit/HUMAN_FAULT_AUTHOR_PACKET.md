# Human fault author packet

## Objective

Author a blind source-to-result corpus that probes contract-guided reverification without access to policy outcomes. The corpus must target semantic failures that can survive structural validation and become observable as a wrong result, silent invalid acceptance or later runtime/backend failure.

## Required corpus

- 15–30 fault cases;
- at least 6 distinct fault families;
- at least 2 valid controls per fault;
- deterministic source, arguments and expected result/diagnostic metadata;
- no case copied from the repository's existing primary, challenge or review-holdout sets.

## Authoring rules

Each fault file must follow `templates/fault-case.schema.json`; each control must follow `templates/control-case.schema.json`. Use globally unique case IDs. Describe the mutation and expected first eligible detection boundary, but do not execute P0/P1/P2/P3 and do not inspect generated experiment results.

The author must attest that they did not receive policy detection matrices, implementation-specific hidden fault operators, prior raw results or reviewer-only holdout answers.

## Freeze

Run:

```bash
python3 freeze_corpus.py <author-directory> <output.tar.gz>
```

A valid author directory contains `faults/`, `controls/` and optional `AUTHOR.md`. The freeze tool validates accounting, canonicalizes JSON, creates a deterministic archive and emits `<output.tar.gz>.sha256`.
