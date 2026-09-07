# DSL evolution study

Controlled micro-evolution experiment comparing a fair clone-and-own implementation with current UniversalToolchain feature composition. The goal is to identify where explicit linguistic reuse starts paying for its ceremony, not to prove UT superiority.

## Identity and environment

- Baseline repository commit: `40117eb68c630f7129c120aaaadc69be8f4ecbfb`
- Frozen hypotheses/workload commit: `3b78658f`
- Isolated E3 propagation commit: `11ea672ed10c6756b52a0494718109f5caf28b45`
- Measurement/results commit: `7baade19a6f263d0bc941df5439fa7ff9415cb7c`
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
