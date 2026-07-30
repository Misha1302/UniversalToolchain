# Frozen study protocol v2

## Research question

How much do executable cross-layer contracts improve detection and boundary localization of integration faults in a runtime-composed DSL compiler, relative to structural/target checks alone and to typed contracts without fail-closed reverification?

## Units and sets

- **Primary inference unit:** operator shape, not identifier-renamed instance.
- **Primary set:** 32 frozen operator shapes represented by 40 raw instances in five families.
- **Challenge set:** 10 author-designed operators whose diagnostic operators are absent from the primary set. Challenge outcomes are reported descriptively and are not pooled into primary significance tests.
- **Negative controls:** 100 valid configurations per mode, with 20 cases in each of five boundary families.

## Modes

- **B0:** AIR structure and target-capability checks only.
- **B1:** B0 plus typed selection, ownership, Bytecode, and facts/effects checks; unresolved reverification is not fatal.
- **B2:** B1 plus fail-closed unresolved reverification.

## Outcomes

A fault is detected only when the expected diagnostic code is emitted at the expected production boundary. Each mutation instance is executed three times in each mode. A run is invalid if a triplet changes detection, diagnostic, or boundary classification, or if instances mapped to one operator shape disagree.

Primary metrics:

1. operator-level detection count and Wilson 95% interval by mode;
2. exact paired McNemar B0 versus B2;
3. exact paired McNemar B1 versus B2;
4. family-level detection and boundary localization.

Challenge metrics are descriptive operator-level counts and Wilson intervals. Negative controls report false positives overall and by family.

## Performance

Boundary-validation timing is secondary evidence. The runner uses 33 samples, 2,000 iterations per sample, and counterbalances B0/B1/B2 order with three rotations. Five independent process runs are analyzed. The report must label this as isolated verifier overhead and must not extrapolate it to end-to-end compilation or runtime execution.

## Validity and stopping rules

The evidence package is invalid if any of the following occurs:

- mixed commit identities;
- missing B0/B1/B2 records;
- incomplete or unstable mutation triplets;
- fewer than 32 primary operator shapes, 10 challenge operators, or 100 controls per mode;
- fewer than five control families;
- missing environment/source evidence or manifest mismatch;
- manual editing of generated results.

Observed outcomes are retained even when unfavorable. The workflow must not fail merely because detection or false-positive results are worse than expected; it fails only for execution, completeness, consistency, or integrity violations.

## Claim boundary

This is an author-designed, single-framework production-boundary study. It does not establish general compiler correctness, effectiveness on externally authored unseen faults, complete source-to-execution coverage, or external validity across other language runtimes. The evidence may support a bounded claim about the exact fault catalogs, production boundaries, modes, and commit recorded in the artifact.
