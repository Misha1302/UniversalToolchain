# UniversalToolchain contract experiment

This non-packable project runs a frozen production-boundary mutation study for the ICSE 2027 SRC work.

The comparison is intentionally narrower than a whole-program correctness claim:

- **B0** keeps existing AIR structure and target-capability checks but omits module ownership, Bytecode drift, and compiler-fact/effect enforcement.
- **B1** adds typed module/ownership/Bytecode/effect checks but does not fail on requested reverification.
- **B2** is the full fail-closed protocol, including mandatory reverification requests.

The catalog contains 40 author-designed faults in five families. Every mutation/mode pair is repeated three times; a changed detection classification fails the run. One hundred clean cases per mode are recorded for false-positive checks. A separate warm benchmark records the cost of boundary validation, but it is secondary evidence and is expected to vary across process-level runs.

## Run

```bash
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  dotnet run -c Release \
  --project UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.ContractExperiments.csproj \
  -- artifacts/contract-experiment
```

Analyze one run plus process-level performance replicates:

```bash
python3 UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/analyze_results.py \
  artifacts/contract-experiment/results.jsonl \
  --replicate-summary artifacts/replicate-1/summary.json \
  --replicate-summary artifacts/replicate-2/summary.json \
  --out-dir artifacts/contract-experiment/analysis
```

## Evidence limitations

- Mutation definitions were designed by the project author.
- The experiment exercises production verifier and contract classes at compiler boundaries; it is not a full mutation of every end-to-end Wist compilation route.
- Results should not be generalized to unrelated compilers without an independent replication.
- The project must not report a test count, detection rate, or overhead unless it is regenerated from the archived raw records for the exact commit.
