# RQ3 downstream/shared-pipeline control results

This is a **post-freeze addendum** to the primary RQ1/RQ2 experiment. It does not change the frozen workload, metrics, raw data, or interpretation of the original study.

## Identity

- Addendum spec freeze: `cf95345b`
- Pre-improvement control state used for measurement: `214204c5668007353033e6c00f2fecca73e39923`
- Isolated downstream-change commit: `5bca2e701a8e213416f0bf1965a5b53b63e9c953`
- Automated evidence commit: `d357155d`

## Control question

Can a feature-independent downstream compiler improvement be implemented once for all variants both in a strong ordinary shared-pipeline baseline and in UniversalToolchain composition?

The baseline intentionally shares the same `PricingProgram` representation across variants. It is therefore a stronger RQ3 competitor than clone-and-own.

## Frozen downstream change

After all selected source-language operations have updated `PricingProgram.Value`, a canonicalization removes the now-consumed `Operations` payload before the backend observes the artifact.

The transformation is semantics-preserving for the experiment: the backend result is `Value`; clearing already-applied operations changes only the terminal representation. The oracle checks both the unchanged numeric value and that the terminal operation count becomes zero.

## Correctness observations

Before the change, both treatments passed the same value oracle and retained the expected operation payload:

| Case | Value | Operations retained before |
|---|---:|---:|
| base | 100 | 0 |
| discount | 90 | 1 |
| surcharge | 110 | 1 |
| both | 99 | 2 |

After the downstream change, both treatments preserved the same values and reported `Operations.Count == 0` for every case.

## Measured isolated-change cost

| Treatment | Changed LOC | Existing files touched | Propagation sites | New downstream LOC | Variant semantic files touched |
|---|---:|---:|---:|---:|---:|
| Ordinary shared pipeline | 3 | 1 | 1 | 3 | 0 |
| UniversalToolchain | 5 | 1 | 1 | 5 | 0 |

The UT treatment spends two additional lines in this micro-control because the canonicalization is registered as an explicit planned pass. The absolute 3-vs-5 LOC difference is too small and implementation-specific to generalize.

The load-bearing result is instead structural: **both treatments implement the downstream improvement at one shared site and touch zero variant-specific semantic files.**

## RQ3 / H4 interpretation

This bounded control supports H4:

> A shared downstream compiler improvement can be implemented once in a well-designed ordinary shared-representation pipeline as well as in UT; therefore shared downstream reuse is a separate benefit from source-language extensibility.

This is a negative/control result for any claim that shared IR or downstream passes uniquely justify UT source composition.

## External framing checked 2026-09-08

- Current MLIR Language Reference states that multiple dialects can coexist in one module and that passes can produce/consume them with conversion within and between dialects: https://mlir.llvm.org/docs/LangRef/
- Current MLIR dialect documentation explicitly supports extensible dialects, but this demonstrates an extensible compiler/IR mechanism rather than proving the need for source-language feature composition: https://mlir.llvm.org/docs/DefiningDialects/
- Current JetBrains MPS FAQ distinguishes language extensions from libraries through syntax, static constraints/type systems, IDE support, and compile-time transformation. A downstream optimization alone therefore does not establish a need for a source-language extension: https://www.jetbrains.com/help/mps/mps-faq.html
- Krüger & Berger, ESEC/FSE 2020, report that platform-oriented reuse has higher development cost while lowering reuse cost, and that change propagation can sometimes be more expensive in a platform. DOI `10.1145/3368089.3409684`.

These sources frame the control; they do not determine its measured result.

## Reproduce

From repository root:

```bash
experiments/dsl-evolution-study/rq3-control/reproduce.sh
```

The script creates detached worktrees for the exact pre-change and downstream-change commits, runs the corresponding retained/canonicalized oracles, and regenerates `results/raw.json` and `results/summary.json`.

## Clean-room verification

Combined detached-worktree replay at `dad0de7b7a06d889cb24963f8b3571087c90216c` regenerated both the primary experiment evidence and the RQ3 evidence with identical SHA-256 hashes and a clean Git status. The replay also served as an integration check that the nested addendum project is isolated from the primary SDK-style project glob.
