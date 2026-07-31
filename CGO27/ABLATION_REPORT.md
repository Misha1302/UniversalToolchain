# CGO 2027 ablation report

Status: `IMPLEMENTED_AND_LOCALLY_VALIDATED_PENDING_PROVIDER`.

| Ablation | Boundary primary | Boundary challenge | Wist early fault rejection | Tensor fault rejection |
|---|---:|---:|---:|---:|
| Remove typed contracts (`P0`) | 12/32 | 1/10 | 0/5 | 0/8 |
| Keep invalidation, remove discharge (`P1`) | 28/32 | 10/10 | 0/5 | 0/8 |
| Selective (`P2`) | 32/32 | 10/10 | 5/5 | 8/8 |

`P2` and `P3` retain parity on 42 boundary shapes, 30 Wist source cases and 12 TensorRules cases. On the 100 boundary controls in the locally validated provider-artifact analysis, P2 executed 120 verifier calls and P3 executed 140, a reduction of 14.3%. This is below the frozen 25% headline threshold and is not whole-compilation timing.

The dedicated workflow regenerates all three input studies on one commit before producing the canonical ablation artifact. Provider-backed status is assigned only after that workflow and its recursive manifest pass.

Removing TensorRules does not change Wist results, but it removes support for the bounded two-package applicability claim. Performance and external-validity claims remain blocked.
