#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$ROOT"
dotnet restore experiments/dsl-evolution-study/DslEvolutionStudy.csproj
dotnet build experiments/dsl-evolution-study/DslEvolutionStudy.csproj -c Release --no-restore
python3 experiments/dsl-evolution-study/scripts/run_oracle.py --treatment control --output experiments/dsl-evolution-study/results/oracle-control.json
python3 experiments/dsl-evolution-study/scripts/run_oracle.py --treatment clone-own --output experiments/dsl-evolution-study/results/oracle-clone-own.json
python3 experiments/dsl-evolution-study/scripts/run_oracle.py --treatment universal-toolchain --output experiments/dsl-evolution-study/results/oracle-universal-toolchain.json
python3 experiments/dsl-evolution-study/scripts/measure.py
python3 experiments/dsl-evolution-study/scripts/make_figures.py
