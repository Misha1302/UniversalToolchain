# CGO 2027 performance environment

Status: `BLOCKED_PINNED_MACHINE` until a dedicated, stable machine is supplied.

## Claim boundary

GitHub-hosted CI and the existing verifier-kernel diagnostic are smoke evidence only. They may not support a whole-compilation efficiency headline. Decision-grade performance evidence must be collected on one declared physical or exclusively reserved virtual machine with a stable identity.

## Required machine contract

The capture runner fails closed unless all of the following are supplied and observed:

- `CGO27_PINNED_MACHINE_ID`: stable human-readable machine identity;
- `CGO27_EXPECTED_CPU`: exact substring expected in `lscpu` model name;
- `CGO27_CPUSET`: explicit logical CPU affinity set;
- Linux CPU governor `performance` for every selected CPU;
- fixed .NET SDK/runtime recorded by `global.json` and `dotnet --info`;
- no frequency-tuning or benchmark process competing on the selected CPUs;
- background one-minute load below the frozen threshold before every cell;
- exact commit, kernel, memory size and microcode recorded in the environment receipt.

## Measurement protocol

- Policies: `P0_STRUCTURAL`, `P1_INVALIDATION`, `P2_SELECTIVE`, `P3_ALWAYS`.
- Workloads: frozen Wist end-to-end strata and TensorRules cases, reported separately.
- Warm-up: 5 unmeasured process executions per cell.
- Measurement: 30 process executions per cell; minimum 15 only when a predeclared power analysis justifies it.
- Scheduling: balanced Latin-square policy order per workload and repetition.
- Cold/warm modes: separate datasets; no pooling.
- Outliers: no deletion by default. Infrastructure-invalid runs are retained and marked; any exclusion rule must be frozen before result inspection.
- Canonical raw data: one JSONL record per process execution.
- Report: median, p95, bootstrap 95% confidence interval, full distribution and verifier invocation counts.

## Efficiency acceptance rule

An efficiency headline is permitted only when:

1. `P2_SELECTIVE` has detection parity with `P3_ALWAYS` on the evaluated fault sets;
2. verifier invocations fall by at least 25% in the nontrivial clean/mixed strata;
3. verification-time reduction is stable across most nontrivial strata;
4. whole-compilation time does not materially regress under the frozen equivalence margin.

Otherwise the paper must remove the efficiency headline and position the contribution around the correctness gap, detection boundary and localization evidence.
