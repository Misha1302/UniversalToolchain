# Contract review holdout protocol

## Purpose

This experiment evaluates four cases derived from an adversarial review performed after the original contract-study primary and challenge corpora were frozen. It is a post-freeze holdout against corpus overfitting. It is not an externally authored or statistically representative compiler-fault sample.

## Frozen cases

1. contract-annotated Bytecode emission without producer identity;
2. contract-annotated Bytecode emission without source-node identity;
3. repeated module identity in a pipeline order whose effects are occurrence-insensitive;
4. invalidated external compiler fact routed through an extension-provided verifier rule.

The case list, expected matrix and denominators are fixed before the workflow result is inspected. Cases must not be merged retroactively into the original 32-operator primary or 10-operator challenge denominators.

## Compared modes

- **B0:** baseline without typed contract checks;
- **B1:** typed metadata/order/routing checks;
- **B2:** B1-compatible holdout observation; fail-closed policy remains evaluated by the main experiment.

Each case runs three deterministic repetitions per mode. Twenty valid Bytecode controls run once per mode. The process fails if a holdout deviates from the frozen matrix or any valid control produces a diagnostic.

## Expected matrix

| Set | B0 | B1 | B2 |
|---|---:|---:|---:|
| Review-derived holdouts | 0/4 | 4/4 | 4/4 |
| Valid-control false positives | 0/20 | 0/20 | 0/20 |

## Claim boundary

A successful run establishes only that the remediated implementation detects these four later-discovered review cases without false positives on the included controls. It does not establish external validity, independent authorship, general compiler correctness, or superiority under equal-budget fuzzing.
