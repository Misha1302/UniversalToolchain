#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
D="${DOTNET:-dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
mkdir -p logs
"$D" build Runner/Runner.csproj -c Release --ignore-failed-sources >logs/repro_build_runner.log 2>&1
Q=$(find ConsumerQ/bin -type f -path '*/Release/net10.0/ConsumerQ.dll' | head -1)
RUN=$(find Runner/bin -type f -path '*/Release/net10.0/Runner.dll' | head -1)
sha256sum "$Q" >logs/repro_Q_before.sha256
"$D" "$RUN" > logs/repro_p1_only.log
"$D" build ProducerP2/ProducerP2.csproj -c Release --ignore-failed-sources >logs/repro_build_P2.log 2>&1
P2=$(find ProducerP2/bin -type f -path '*/Release/net10.0/ProducerP2.dll' | head -1)
"$D" "$RUN" "$PWD/$P2" > logs/repro_p1_p2_q.log
sha256sum "$Q" >logs/repro_Q_after.sha256
diff -u logs/repro_Q_before.sha256 logs/repro_Q_after.sha256
grep -q 'P2_NOT_LOADED checks=1' logs/repro_p1_only.log
grep -q 'P2_LOADED=YES checks=15 false_accepts=0' logs/repro_p1_p2_q.log
printf 'REPRO_SUCCESS; Q_DLL_IDENTICAL=YES\n'
cat logs/repro_p1_p2_q.log
