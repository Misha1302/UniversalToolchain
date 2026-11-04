# DSL evolution study

Controlled micro-evolution experiment comparing a fair clone-and-own implementation with current UniversalToolchain feature composition. The goal is to identify where explicit linguistic reuse starts paying for its ceremony, not to prove UT superiority.

## Identity and environment

- Baseline repository commit: `40117eb68c630f7129c120aaaadc69be8f4ecbfb`
- Frozen hypotheses/workload commit: `3b78658f`
- Isolated E3 propagation commit: `11ea672ed10c6756b52a0494718109f5caf28b45`
- Measurement/results commit: `7baade19a6f263d0bc941df5439fa7ff9415cb7c`
- Post-freeze RQ3 spec: `cf95345b`
- RQ3 pre-improvement state: `214204c5668007353033e6c00f2fecca73e39923`
- RQ3 isolated downstream change: `5bca2e701a8e213416f0bf1965a5b53b63e9c953`
- Branch: `research/dsl-evolution-experiment-2026-09-08`
- OS: Fedora Linux 43 x64
- .NET SDK used: `10.0.111`; runtime `10.0.11`
- Python: `3.14.7`
- Git: `2.55.0`

## Subject

The language accepts `price <decimal>` followed by pipe-separated percentage operations. The frozen workload adds discount, then surcharge/combined composition, then a shared percent-range rule. UT treatment uses existing `LanguagePackageBuilder -> LanguageCompiler -> LanguagePlan -> LanguageRuntime` mechanisms. No UT core code was modified.

The experiment boundary is semantic feature/pass composition after a small parser. It does **not** evaluate arbitrary independent grammar composition because the current generic SDK explicitly lacks high-level generic grammar/binder/type-system authoring.

## Reproduce

From repository root:

```bash
dotnet restore experiments/dsl-evolution-study/DslEvolutionStudy.csproj
dotnet build experiments/dsl-evolution-study/DslEvolutionStudy.csproj -c Release --no-restore
experiments/dsl-evolution-study/scripts/reproduce.sh
```

Individual oracle checks:

```bash
python3 experiments/dsl-evolution-study/scripts/run_oracle.py --treatment control
python3 experiments/dsl-evolution-study/scripts/run_oracle.py --treatment clone-own
python3 experiments/dsl-evolution-study/scripts/run_oracle.py --treatment universal-toolchain
```

Regenerate measurements and figures:

```bash
python3 experiments/dsl-evolution-study/scripts/measure.py
python3 experiments/dsl-evolution-study/scripts/make_figures.py
```

Expected outputs are under `experiments/dsl-evolution-study/results/`: treatment oracle JSON, `raw.json`, `raw.csv`, `summary.json`, and two SVG figures.

Post-freeze RQ3 shared-pipeline control:

```bash
experiments/dsl-evolution-study/rq3-control/reproduce.sh
```

Its independent evidence is under `experiments/dsl-evolution-study/rq3-control/results/`. It does not rewrite the primary RQ1/RQ2 results.

## Repository checks executed

```bash
dotnet run --project samples/Acme.PricingLanguage/Acme.PricingLanguage.csproj -c Release
dotnet test UniversalToolchain/UniversalToolchain.LanguageSdk.Tests/UniversalToolchain.LanguageSdk.Tests.csproj -c Release
dotnet test UniversalToolchain/UniversalToolchain.LanguageSdk.Generic.Tests/UniversalToolchain.LanguageSdk.Generic.Tests.csproj -c Release
```

Observed: Acme output `35.0:35.0`; LanguageSdk tests 185/185 pass; Generic LanguageSdk tests 62/62 pass.

## Claim map

- `RESULTS.md`: measured observations and hypothesis status.
- `LIMITATIONS.md`: what the experiment does not measure.
- `THREATS_TO_VALIDITY.md`: adversarial baseline and measurement review.
- `LANGDEV_TAKEAWAYS.md`: presentation-safe extraction.
- `RQ3_RESULTS.md`: isolated post-freeze shared-downstream control and its interpretation.
- `results/summary.json`: machine-readable metric source.

## External framing anchors

These sources motivate the problem/counterarguments; they do not determine the experimental result.

1. Borum & Seidl, MODELS 2022, DOI `10.1145/3550355.3552413`.
2. Zhang, Strüber & Hebig, Empirical Software Engineering 31:48 (2026), DOI `10.1007/s10664-025-10775-2`.
3. Bertolotti, Cazzola & Favalli, JSS 202 (2023) 111704, DOI `10.1016/j.jss.2023.111704`.
4. Krüger & Berger, ESEC/FSE 2020, DOI `10.1145/3368089.3409684`.
5. Völter et al., OOPSLA 2015, DOI `10.1145/2814270.2814276`.
6. Current JetBrains MPS FAQ, “Why extend a language? Aren't libraries good enough?”.
7. Current MLIR documentation on dialects/extensible dialects.
8. Martin Fowler, “Is Design Dead?” and “YAGNI”.

## Clean-room reproduction

A detached clean worktree at `2211af5e723b4f11ec8cf604cd93281fb0d02d35` ran `scripts/reproduce.sh` successfully. Regenerated `raw.json`, `raw.csv`, `summary.json`, and all three oracle JSON files were byte-for-byte SHA-256 identical to the committed artifacts, and the worktree remained clean after reproduction.

The first clean-room attempt exposed a CSV newline nondeterminism (Python `csv` CRLF output versus Git-normalized LF). It did not change any values; commit `2211af5e` fixes the writer with an explicit LF terminator and the clean-room check was repeated successfully.

### Combined clean-room replay after RQ3 addendum

A detached clean worktree at `dad0de7b7a06d889cb24963f8b3571087c90216c` ran both the primary `scripts/reproduce.sh` and the isolated `rq3-control/reproduce.sh`. SHA-256 hashes of all committed primary raw/summary/oracle artifacts and all RQ3 raw/summary/oracle artifacts were identical before and after regeneration, and the clean worktree remained clean.

The first combined replay exposed an SDK-style default-glob integration defect: the primary `DslEvolutionStudy.csproj` recursively included the nested RQ3 project's top-level `Program.cs` and generated `obj/*.cs`. Commit `dad0de7b` fixes only that packaging boundary by excluding `rq3-control/**/*.cs` from the primary project; it does not alter the frozen primary treatment implementations or metrics.
