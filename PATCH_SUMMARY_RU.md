# Patch summary: Wist independent-review remediation

## Runtime and policy

- Unified backend selection across every public facade operation.
- Validated preset/backend combinations before engine construction completes.
- Internalized raw Wist execution and compatibility paths that bypassed facade policy.
- Replaced process-wide simple-name loading with root-authoritative isolated runtime contexts.
- Bumped incompatible runtime assembly generation to `2.0.0.0`.
- Made unknown identifiers and declared types fail closed.
- Stabilized observable local/label identities and normalized runtime-owned values at public boundaries, including `object` results.
- Removed one-shot `AsyncLocal` retention so disposed isolated runtimes are collectible.
- Adapted CLR numeric parameters to preset-owned numeric representations.

## Package and release

- Added exact reviewed Wist managed closure and runtime-manifest SHA-256 map.
- Added zero-byte, missing assembly, unexpected assembly, alias-drift, managed-identity-swap and preset-semantic-swap mutants.
- Bound package assemblies to exact compiler/runtime build outputs and presets to reviewed SHA-256 values.
- Made the canonical test contract enforce exact TRX counts, outcomes and per-suite timeouts.
- Added a 74-entry API/package compatibility ledger.
- Restored every declared package project before `pack --no-restore`.
- Added clean Wist and Language SDK consumers with empty caches.
- Added incompatible-checkout rejection and detached release-integrity mutants.

## Verification

- 1545/1545 tests passed across six suites.
- 9 package projects packed.
- 6/6 Wist package mutants rejected.
- Wist and Language SDK clean consumers passed.
- 10 release package artifacts verified against a detached root.
- Documentation status and navigation/link checks passed.

See `SECOND_REMEDIATION_REPORT_RU.md` and `SECOND_REREVIEW_REMEDIATION_STATUS.csv`.
