# CGO 2027 ablation report

Status: `PROVIDER_BACKED_VALIDATED`.

| Ablation | Boundary primary | Boundary challenge | System W early fault rejection | Tensor fault rejection |
|---|---:|---:|---:|---:|
| Remove typed contracts (`P0`) | 12/32 | 1/10 | 0/5 | 0/8 |
| Keep invalidation, remove discharge (`P1`) | 28/32 | 10/10 | 0/5 | 0/8 |
| Selective (`P2`) | 32/32 | 10/10 | 5/5 | 8/8 |

`P2` and `P3` retain parity on 42 boundary shapes, 30 System W source cases and 12 TensorRules cases. On the 100 boundary controls, P2 executed 120 verifier calls and P3 executed 140, a reduction of 14.3%. This is below the frozen 25% headline threshold and is not whole-compilation timing.

## Provider receipt

- branch head: `f6d9b830105609306bf698732b00ea2fa2341c99`;
- workflow: `CGO27 Ablations`, run `30662796314`;
- artifact ID: `8805860476`;
- artifact digest: `sha256:ad6097480099a370499e7464f28013a6cdc888b639813d1d28d8d8fa964e19`;
- regenerated input commit recorded by the artifact: `1af9335e57553865a8fcd79ddd66a3f987c39e91`;
- recursive outer and nested checksum manifests: PASS.

Removing TensorRules does not change System W results, but it removes support for the bounded two-package applicability claim. Performance and external-validity claims remain blocked.
