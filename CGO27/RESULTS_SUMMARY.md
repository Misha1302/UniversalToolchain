# CGO 2027 results summary

Status: validated evidence summary; final exact-head provider receipt pending.

## Boundary study

The historical Wist boundary corpus preserves its identifiers and denominators; the demand pair is versioned separately.

| Corpus | P0 | P1 | P1D | P2 | P3 |
|---|---:|---:|---:|---:|---:|
| Primary operator shapes | 12/32 | 28/32 | 28/32 | 32/32 | 32/32 |
| Challenge operators | 1/10 | 10/10 | 10/10 | 10/10 | 10/10 |
| Demand pair | 0/2 | 0/2 | 1/2 | 2/2 | 2/2 |
| Valid controls rejected | 0/120 | 0/120 | 0/120 | 0/120 | 0/120 |

P2 and P3 agree on outcome, diagnostic family and first detection boundary for all 42 historical primary/challenge shapes and both demand cases. P1D rejects only the explicitly queried member of the demand pair.

## Wist source-to-result study

Schema v3 contains:

- 32 source programs: the historical 30-case denominator plus two separately reported demand cases;
- five policies and two fresh-process repetitions per case/policy;
- 320 raw records;
- seven targeted faults and 25 valid controls;
- P07 repaired and accepted by all five policies;
- P2/P3 classification parity 32/32;
- P1D rejects D01 (queried) and permits D02 (unqueried), while P2/P3 reject both.

For the five historical optimizer faults, P0/P1/P1D allow a wrong result or later failure and P2/P3 reject at the optimized-AIR contract boundary. Demand cases are not retroactively added to the historical denominator.

## TensorRules second-language package

Schema v2 contains 14 cases and 70 observations: two valid, two intrinsically invalid, eight historical faults, and two demand cases. P2/P3 parity is 14/14. P1D rejects only the queried demand case; P2/P3 reject both. TensorRules uses the public SDK, has zero Wist references, and is model-authored rather than independent.

## Ablations and work counts

The full protocol detects all eight isolated counterexamples; removing each corresponding mechanism loses its detection, and matched controls remain 0/8 false positives. On 120 boundary controls, P2 performs 120 verifier calls and P3 performs 160, a 25% reduction. This is isolated verifier-work evidence only.

## Exact-head verification

The current repair is being revalidated on a new exact branch head. No final workflow receipt is claimed until Ablations, Artifact, paper, canonical tests and architecture/documentation gates all succeed on the same revision.

## Blocked claims

- Whole-compilation performance remains `BLOCKED_PINNED_MACHINE`.
- Independent external validity remains `BLOCKED_EXTERNAL`.
- Finite parity observations do not prove general P2/P3 equivalence.
