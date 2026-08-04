# Adversarial review 2 — experiment and evidence integrity

Review type: model-authored adversarial pass.
Scope: boundary v4, source-to-result v3, System T v2, mechanism/policy ablations, P07 repair, historical screening, claim ledger, and raw-data lineage.

## Acceptance criteria

Every reported denominator must be recoverable from versioned raw data; controls must remain in the denominator; a pre-existing baseline defect must be repaired rather than removed; historical candidates must remain accounted for even when replay is blocked.

## Findings and resolution

1. **Demand cases were initially mixed into the historical mutation catalog.** The historical catalog and SHA are restored byte-for-byte; demand mutations now have a separate v4 catalog and checksum.
2. **Schema declarations lagged the new corpus.** Boundary, source-to-result, System T, analyzer, renderer, artifact expectations, and paper tables now use their explicit v4/v3/v2 contracts.
3. **The ablation aggregator retained stale 240/48/four-policy cardinalities.** It now requires 320 source observations, 70 System T observations, five policies, and separate historical/demand IDs.
4. **P07 was an all-policy baseline failure.** The numeric-promotion defect was fixed separately, the same case and oracle were retained, and the versioned matrix reran.
5. **Historical screening counts and timestamps diverged across files.** The final predeclared accounting is 24 total: 3 included, 11 excluded, 10 resource/aggregation-blocked, 0 silently dropped.
6. **Exact historical replay could have been overclaimed.** The attempt is recorded as `BLOCKED_RESOURCE`; only the pre-study fresh-process evidence is used.

## Evidence boundaries

- P2/P3 parity is functional, not a performance result.
- The 25% reduction is isolated verifier work on 120 controls, not whole-compilation speedup.
- System T is a second package, not independently authored.
- No external-human corpus or external-validity claim exists.

## Residual limitations

`BLOCKED_EXTERNAL` and `BLOCKED_PINNED_MACHINE` remain. They remove external-validity and whole-compilation efficiency claims, but do not invalidate the bounded detection and parity results.

## Verdict

`PASS_BOUNDED`: denominators, versioning, raw evidence, tables, historical accounting, and non-claims are mutually consistent. No unresolved evidence inflation remains.
