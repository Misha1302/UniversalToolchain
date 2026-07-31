# CGO 2027 research artifact

This artifact reproduces the evaluated, non-performance claims for contract-guided reverification.

## Quick check

From a repository checkout or a clean unpacked artifact:

```bash
bash quick-check.sh artifacts/quick-check
```

The wrapper extracts the embedded source archive when necessary. It rebuilds and runs the Wist boundary study, the 30-case Wist source-to-result study, the TensorRules second-language study and the cross-study ablation analyzer. The expected completion marker is `CGO27_ARTIFACT_QUICK_CHECK=PASS`.

## Full reproduction

```bash
bash reproduce.sh artifacts/full
```

Full reproduction runs the quick check and builds the anonymous paper when `pdflatex` is available. Decision-grade whole-compilation performance is intentionally excluded because it requires the pinned-machine contract in `PERFORMANCE_ENVIRONMENT.md`.

## Contents

- `source/`: deterministic source archive for the exact commit;
- provider workflow output: raw/checksummed quick-check evidence generated beside the static bundle;
- `paper/source/`: anonymous paper source in the static bundle; the provider-built PDF is uploaded beside the bundle;
- `protocols/`: experiment, claim, result, deviation, performance and external-author contracts;
- `quick-check.sh`, `reproduce.sh`, and `Dockerfile`;
- `MANIFEST.sha256` and top-level bundle checksum.

The static tarball is deterministic for a fixed commit because run-specific evidence and the generated PDF are uploaded beside it rather than embedded.

The external blind corpus is not included because no external author has supplied one. The author/freeze/import kit is included, and the corresponding claim remains `BLOCKED_EXTERNAL`.
