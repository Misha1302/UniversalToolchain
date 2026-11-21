# Cross-Backend Parity Corpus (UT-A050)

This document describes how the repository verifies that the two shipped backends mean the same thing,
and — importantly — which asymmetries are *known and pinned* rather than silently ignored.

It is the executable form of the "semantic parity contract" in
[Backends and Semantic Parity](./backends-and-parity.md).

## The defect this closes

Before this fixture, the parity claim rested on hand-written tests that each pinned **one** backend.
That shape cannot fail for the reason it exists: if the interpreter and the CIL path drift apart, the
interpreter test stays green and the CIL test stays green, because neither test ever compares them.

Parity is a *relation between two subjects*, so it has to be measured as one. The corpus does that.

## What is measured

| Axis | Values |
| --- | --- |
| Preset | every preset that declares **both** backends (5 at the time of writing) |
| Source | 18 constructs: arithmetic, native and floating point, overflow, division by zero, comparisons, booleans, variables, a loop |
| Backend | `cil`, `interpreter` |
| SSA policy | `Disabled`, `Prefer`, `Require` |
| Execution path | `Evaluate` (artifact executor) and `Compile` (durable program) |

That is 90 corpus rows and 4 measurements per row before any metamorphic check.

### Observation, not exception text

An outcome is compared as `(exact CLR type, value)` or, for a failure, as the **root cause** type:

```text
value(Double:5)   failure(DivideByZeroException)
```

The wrapper exception is deliberately **not** compared. The interpreter adds instruction context and the
CIL path adds a reflection frame (`TargetInvocationException` from `DynamicMethod.Invoke`), and the facade's
documented compatibility key is `FailureKind` plus diagnostic codes, not the outer type. Comparing
envelopes would test the wrapper and would pin an accident of `System.Reflection`.

The CLR type is part of the observation on purpose: `Int32:3` and `Double:3` must not compare equal, or a
silent numeric-model drift would pass.

### Two execution paths, both measured

`Evaluate` runs the registered artifact executor; `Compile` materializes a durable program. They are two
independent implementations of the same AIR. This was found by mutation: injecting `+1` into the durable
path left every other assertion in the fixture green, because they only exercised the executor path. The
`CompilePath_AgreesWithEvaluatePathAndWithTheCorpus` test closes it.

## The corpus is committed data

`ParityCorpus.json` is checked in. `ParityCorpusTests` reads it and compares it against live measurements;
`ParityCorpusGenerator.CommittedCorpus_EqualsFreshMeasurement` re-measures everything and fails when the
committed file no longer matches.

A corpus computed only at test time agrees with whatever the code currently does, so it can never fail — the
same reason the plan-hash goldens are committed. Here the golden *is* re-derived on every run, but it is
compared against the committed file rather than against itself, so an unintended behaviour change fails the
suite while an intended one is a reviewable diff.

Regenerating is a deliberate act:

```bash ci-run=false
WIST_REGENERATE_PARITY_CORPUS=1 dotnet test \
  UniversalToolchain/UniversalToolchain.Dialects.Tests \
  --filter FullyQualifiedName~ParityCorpusGenerator
```

The block above is deliberately not executed by the markdown smoke check. `WIST_REGENERATE_PARITY_CORPUS=1`
makes the fixture **write** the corpus instead of comparing it, so running it in CI would replace the golden
with whatever the code currently produces — the check would pass by erasing the evidence it exists to
produce. The canonical test contract already runs the same fixture without the variable and compares against
the committed file; that is the check. This block is only the recipe for a deliberate, reviewed update.

Then read `git diff` and decide whether that is the change you meant to make. This is an environment switch
rather than an `[Explicit]` attribute on purpose: an explicit fixture is reported as `NotExecuted`, which the
canonical test contract rejects, and a golden that is never compared is not a golden.

The measurement **refuses to proceed** when it already contradicts the parity claim. If `Disabled` or
`Prefer` disagree across backends, if `Prefer` changes the answer relative to `Disabled`, or if `Compile`
disagrees with `Evaluate`, it throws before anything is written.

## Known asymmetries, pinned explicitly

Two asymmetries exist today. They are recorded, not hidden.

### 1. `Require` SSA is a capability axis, not a semantic one

Under `WistSsaPolicy.Require` the SSA pack refuses AIR that carry an intrinsic it does not model. The two
backends emit different AIR for the same source, so the refusal is asymmetric:

| Source | CIL | Interpreter | Reason |
| --- | --- | --- | --- |
| `1 / 0`, `1.0 / 0.0`, `0.0 / 0.0` | refuses | executes | CIL emits `div_i32`, which `AirCoreIntrinsicDescriptors.ArithmeticInt32` does not model |
| `2 == 2` | executes | refuses | the interpreter's comparison path emits `call C#`, which the pack does not model |
| `2 < 3` | refuses (`cmp_lt_i32`) | refuses (`call C#`) | both refuse, so they agree |

`ssa` preset (which defaults to `Require`) and the two `full-default*` presets therefore expose this on
different sources. `pricing-restricted` shows it for division only.

This is **not** a semantic divergence: no source evaluates to two different values. It is an incomplete SSA
intrinsic model, and `ParityCorpusTests` pins its exact size — 10 of 90 rows. The test fails both when a new
divergence appears (a capability regression) and when one disappears (the asymmetry was fixed and the record
is stale). Neither may pass silently.

### 2. The CIL path leaks `TargetInvocationException`

For a *runtime* failure such as `1 / 0`, the CIL backend surfaces `TargetInvocationException` wrapping
`DivideByZeroException`, while the interpreter surfaces its own `RuntimeExecutionException`. The root cause
is identical, which is what the corpus compares. The wrapper difference is real and is a defect in
observability, not in semantics; it is recorded here rather than being papered over by comparing root causes
and calling the surface uniform.

## Metamorphic relations

`MetamorphicParityTests` covers the transformations that must not change meaning. Each assertion evaluates
both forms and compares them; separate tests for each spelling could not fail for divergence.

| Relation | Scope |
| --- | --- |
| `;` ≡ newline | presets that declare `wist.semicolon-as-new-line` |
| `;` is **not** a separator | presets that do not declare it |
| whitespace, redundant parens, constant fold | all dual-backend presets |
| `a + b` ≡ `b + a`, `a * b` ≡ `b * a` | all dual-backend presets |

Semicolon-as-newline is a **declared feature**, so the fixture partitions the presets by declaration and
asserts the opposite in each half. A preset that accepted `;` without declaring the feature would implement
more than its feature list advertises, and a consumer reading that list would be misled.

Both halves are proven able to fire:

- making the semicolon module universal (leaking it into another frontend module) fails
  `Semicolon_IsNotASeparatorWhereTheFeatureIsNotDeclared` and
  `Semicolon_IsTheReason_TheNonDeclaringPresetsRejectIt`;
- making `;` lex as a non-newline token fails `SemicolonAndNewline_AgreeWhereTheFeatureIsDeclared`.

## Guards against green blanks

A differential test that compares nothing is green forever. Each loop therefore asserts that it ran:

- `Corpus_CoversEveryDualBackendPresetAndBothBackends` — every dual-backend preset appears, row count is
  exactly `presets x sources`, row keys are unique, and the preset set did not shrink below 5;
- `WhitespaceAndParentheses_DoNotChangeTheAnswer` — asserts at least 40 comparisons ran (measured: 40);
- `CommutativeOperands_AgreeForAdditionAndMultiplication` — asserts at least one comparison ran;
- `NegativeHalfPredicate_FiresOnTheShapesItMust` — table test over the negative-half predicate;
- `Harness_DetectsAChangedMeaning` and `Harness_DetectsInjectedDivergence` — compare observations known to
  differ and require the comparison to report it, through the real probe path.

## Mutation evidence

The fixture was verified by killing mutants, not by being green:

| Injected defect | Killed by |
| --- | --- |
| one recorded value changed | `EveryCorpusRow_MatchesItsRecordedExpectation` |
| corpus shrunk to one preset | `Corpus_CoversEveryDualBackendPresetAndBothBackends` |
| understated `DeclaredRequireDivergences` | `RequirePolicy_DivergencesAreExactlyTheOnesOnRecord` |
| a `Require` divergence rewritten to look equal | `RequirePolicy_DivergencesAreExactlyTheOnesOnRecord` |
| `+1` on int results in the interpreter **executor** | 3 parity tests |
| `+1` on int results in the **durable** interpreter path | `CompilePath_AgreesWithEvaluatePathAndWithTheCorpus` |
| `;` mapped to a non-newline tag | `SemicolonAndNewline_AgreeWhereTheFeatureIsDeclared` |
| `;` leaked into an unrelated frontend module | both negative-half tests |

The last four are the reason the corpus covers two execution paths, and the reason the semicolon relation is
tested in both directions.

## Adding a source

1. Add it to `ParityCorpusGenerator.Sources`.
2. Run `ParityCorpusGenerator` and read the diff it reports.
3. If the measurement throws, the new source found a divergence. Fix it, or add it to the pinned record with
   a rationale — do not weaken the generator.
4. Regenerate with `WIST_REGENERATE_PARITY_CORPUS=1` and commit the regenerated `ParityCorpus.json` together
   with the new source.
