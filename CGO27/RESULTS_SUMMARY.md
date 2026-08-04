# CGO 2027 results summary

Status: validated bounded evidence summary. Exact-head provider identities are recorded in the draft PR and delivered readiness report rather than embedded recursively in the source revision.

## Boundary study

The historical System W boundary corpus preserves its identifiers and denominators; the demand pair is versioned separately.

| Corpus | P0 | P1 | P1D | P2 | P3 |
|---|---:|---:|---:|---:|---:|
| Primary operator shapes | 12/32 | 28/32 | 28/32 | 32/32 | 32/32 |
| Challenge operators | 1/10 | 10/10 | 10/10 | 10/10 | 10/10 |
| Demand pair | 0/2 | 0/2 | 1/2 | 2/2 | 2/2 |
| Valid controls rejected | 0/120 | 0/120 | 0/120 | 0/120 | 0/120 |

P2 and P3 agree on outcome, diagnostic family, and first detection boundary for all 42 historical primary/challenge shapes and both demand cases. P1D rejects only the explicitly queried member of the demand pair.

## System W source-to-result study

Schema v3 contains:

- 32 source programs: the historical 30-case denominator plus two separately reported demand cases;
- five policies and two fresh-process repetitions per case/policy;
- 320 raw records;
- seven targeted faults and 25 valid controls;
- P07 repaired and accepted by all five policies without changing source or oracle;
- P2/P3 classification, diagnostic-family, and first-boundary parity 32/32;
- P1D rejects the queried demand case and permits the matched unqueried case, while P2/P3 reject both.

For the five historical optimizer faults, P0/P1/P1D allow a wrong result or later failure and P2/P3 reject at the optimized-AIR contract boundary. Demand cases are not retroactively added to the historical denominator.

## System T public-SDK package

Schema v2 contains 14 cases and 70 observations: two valid, two intrinsically invalid, eight historical study faults, and two demand cases. P2/P3 parity is 14/14. P1D rejects only the queried demand case; P2/P3 reject both. System T uses the public SDK and a different shape/layout vocabulary, but it shares scheduler code and is study-authored rather than independent.

## Historical screening

The frozen accounting contains 24 candidates:

- 3 included pre-study issue-defined semantic defects with original source/oracle/fix provenance and stable pre-study confirmation;
- 11 excluded with retained predeclared reasons;
- 10 blocked aggregate fixes without a bounded one-root reproducer and independently derivable oracle in the available export;
- 0 invalid cases silently removed.

A new exact-revision replay was attempted but terminated during old dependency-graph compilation before policy execution. The historical set therefore supports provenance and feasibility claims only; it contributes no new P2 detection rate and is not described as independent.

## Ablations and work counts

The full protocol detects all eight isolated counterexamples; removing each corresponding mechanism loses its detection, and matched controls remain 0/8 false positives. On 120 boundary controls, P2 performs 120 verifier calls and P3 performs 160, a 25% reduction. This is isolated verifier-work evidence only, not a whole-compilation speedup result.

## Verification boundary

The final delivered branch head is acceptable only when all 13 required exact-head workflows succeed on the same revision and the provider paper, archival artifact, anonymous supplement, and exact-source artifacts pass independent manifest/content inspection. The draft PR and delivered readiness report contain those non-recursive identities.

## Blocked claims

- Whole-compilation performance remains `BLOCKED_PINNED_MACHINE`.
- Independent external validity remains `BLOCKED_EXTERNAL`.
- Historical policy replay remains `BLOCKED_RESOURCE`.
- Finite parity observations do not prove general P2/P3 equivalence.
