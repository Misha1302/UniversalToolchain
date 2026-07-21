# UniversalToolchain contract experiment

This non-packable project runs a frozen production-boundary mutation study for the ICSE 2027 SRC work.

The comparison is intentionally narrower than a whole-program correctness claim:

- **B0** keeps existing AIR structure and target-capability checks but omits module ownership, Bytecode drift, and compiler-fact/effect enforcement.
- **B1** adds typed module/ownership/Bytecode/effect checks but does not fail on requested reverification.
- **B2** is the full fail-closed protocol, including mandatory reverification requests.

The catalog contains 40 author-designed fault instances in five families. Bytecode and capability pairs intentionally repeat the same operator shape with independent identifiers; the submission evidence therefore reports both all 40 instances and a conservative analysis collapsed to 32 operator shapes. Every mutation/mode pair is repeated three times, and a changed detection classification fails the run. One hundred clean cases per mode are recorded for false-positive checks. A separate warm benchmark records boundary-validation cost as secondary evidence.

## Run

The following commands are intentionally excluded from the repository-wide Markdown smoke job because the canonical CI workflow already executes the complete study and archives its evidence.

```bash ci-run=false
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  dotnet run -c Release \
  --project UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.ContractExperiments.csproj \
  -- artifacts/contract-experiment
```

Run the multi-process experiment and analysis:

```bash ci-run=false
ICSE_EXPERIMENT_REPLICATES=5 \
  ./Tools/run-contract-experiment.sh artifacts/contract-experiment
```

The runner validates raw-record completeness, one-commit identity, stable triplets, and its evidence manifest. The submission package additionally contains the operator-shape reanalysis used to avoid treating paired identifier variants as independent operators.

## Evidence limitations

- Mutation definitions were designed by the project author.
- The experiment exercises production verifier and contract classes at compiler boundaries; it is not a full mutation of every end-to-end Wist compilation route.
- Paired identifier variants are descriptive replications, not independent operator classes.
- Results should not be generalized to unrelated compilers without an independent replication.
- The project must not report a test count, detection rate, or overhead unless it is regenerated from the archived raw records for the exact commit.