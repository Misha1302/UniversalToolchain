# CGO 2027 research artifact

This artifact reproduces the evaluated, non-performance claims for contract-guided reverification.

## Quick check

From a repository checkout or a clean unpacked artifact:

```bash ci-run=false
bash quick-check.sh artifacts/quick-check
```

The wrapper verifies the artifact manifest, resolves the exact source revision, and extracts the embedded source archive when necessary. It rebuilds and runs the boundary study, the 32-case source-to-result study, including the matched demand pair, the TensorRules second-language study, eight isolated mechanism ablations, and the cross-study analyzer. It regenerates the paper tables and byte-compares them with the anonymous committed sources. The expected completion marker is `CGO27_ARTIFACT_QUICK_CHECK=PASS`.

## Full reproduction

```bash ci-run=false
bash reproduce.sh artifacts/full
```

Full reproduction runs the quick check and builds the anonymous paper when `pdflatex` is available. Decision-grade whole-compilation performance is intentionally excluded because it requires the pinned-machine contract in `PERFORMANCE_ENVIRONMENT.md`.

## Contents

- `source/`: deterministic non-anonymous archival source for the exact commit;
- provider workflow output: raw/checksummed quick-check evidence generated beside the static bundle;
- provider-built `cgo27-anonymous-supplement.tar.gz`: neutral double-blind package with selected raw evidence, analyzers, protocols, and anonymous paper source;
- `paper/source/`: self-contained anonymous paper source, including evidence-generated tables; the provider-built PDF is uploaded beside the bundle;
- `protocols/`: experiment, claim, result, deviation, performance and external-author contracts;
- `quick-check.sh`, `reproduce.sh`, and `Dockerfile`;
- `COMMIT`, `MANIFEST.sha256`, and top-level bundle checksum.

The static tarball is deterministic for a fixed commit because run-specific evidence and the generated PDF are uploaded beside it rather than embedded. Repository runs derive identity from Git HEAD. Clean-unpack runs derive it from the manifest-covered `COMMIT` receipt because `git archive` intentionally omits `.git` metadata.

The external blind corpus is not included because no external author has supplied one. The author/freeze/import kit is included, and the corresponding claim remains `BLOCKED_EXTERNAL`.

The archival artifact and the anonymous supplementary package are distinct deliverables. Only the latter is suitable for double-blind upload.
