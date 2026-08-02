# CGO 2027 experiment protocol index

This document identifies canonical protocol owners; it does not duplicate their detailed rules.

## Studies

1. **Wist boundary study**
   - protocol: `UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md`
   - raw schema: `raw-result-schema-v3.json`
   - oracle: `oracles-v3.json`
   - historical v2 protocol remains immutable.

2. **Wist source-to-result study**
   - runner: `UniversalToolchain/Experiments/UniversalToolchain.EndToEndExperiments/run_matrix_v2.py`
   - execution entrypoint: `Tools/run-cgo27-end-to-end.sh`
   - 30 cases, four policies, two fresh-process repetitions
   - provider receipt: workflow `30661725052`, artifact `8805491648`.

3. **TensorRules second-language study**
   - route and oracle: `UniversalToolchain/Experiments/UniversalToolchain.TensorRules/Program.cs`
   - execution entrypoint: `UniversalToolchain/Experiments/UniversalToolchain.TensorRules/run.sh`
   - 12 cases and 48 observations
   - provider receipt: workflow `30661725387`, artifact `8805405891`.

4. **External blind corpus**
   - author/freeze/import kit: `CGO27/external-fault-kit/`
   - execution prohibited until a validated external archive receipt exists.

5. **Performance**
   - environment contract: `CGO27/PERFORMANCE_ENVIRONMENT.md`
   - capture gate: `Tools/capture-cgo27-performance-environment.sh`
   - decision-grade execution prohibited until the pinned environment gate passes.

## Cross-study invariants

- P2/P3 parity is checked separately for every dataset.
- New datasets never enter historical denominators.
- Raw records are canonical and written before result assertions.
- Model-authored and externally authored corpora are never pooled without separate labels.
- Infrastructure failures, baseline runtime failures and protocol detections have separate classifications.
