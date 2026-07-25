# Implementation baseline

## Источник истины

- Handoff: `wist-remediation-agent-handoff-2026-07-25(1).zip`.
- Неизменённый baseline: `source/Wist/` внутри handoff.
- SHA-256 исходного `Wist.zip`: `0c6e4551607d8b21b0455454d5c0c34b23bf3c19eb0d086adb0f9bf884d218ff`.
- Git baseline commit рабочего дерева: `b2058f803a678d673398e834eab88f152f37c4c7`.
- Исходный `MANIFEST.sha256`: 1657/1657 записей совпали до внесения изменений.
- Рабочее дерево создано отдельно; handoff/source не изменялся.

## Среда

- Debian GNU/Linux 13, `linux-x64`.
- .NET SDK `10.0.301`, MSBuild `18.6.4`, host/runtime `10.0.9`.
- Offline NuGet cache: `/mnt/data/nuget-sidecar/packages`.
- Repository inventory: 92 `.csproj`, 2 `.sln` (`Wist.sln`, `PlanFuzz.sln`), 1 `.slnx`, 6 test projects, 1322 production/test C# files без `bin/obj`.

## Baseline execution status

- Внутренний manifest baseline проверен полностью.
- Offline restore baseline прошёл.
- Один ограниченный общий baseline build не завершился в пределах execution window и не был засчитан как успешный или неуспешный; зафиксированных compile errors до остановки не было.
- Исходные runtime reproductions F-01…F-09 и evidence F-10…F-14 взяты из handoff-аудита и перенесены/адаптированы в repository regression tests.
- Финальный исправленный код проверен полными Release builds, всеми шестью test projects и package/consumer gates; результаты приведены в `REPRO_RESULTS.txt` и `IMPLEMENTATION_REPORT_RU.md`.

## Исходные критические инварианты

1. Исполняется только canonical verified plan и точный выбранный component set.
2. Wist runtime принимает registry-issued package provenance, а не самодекларируемые id/version/hash.
3. Metadata не является исполняемым DSL.
4. Feature ID соответствует реальной semantic capability.
5. Verifier блокирует нарушение или оставляет observable diagnostic.
6. Reentrant dispose не ждёт собственную operation lease.
7. Runtime identity scoped/frozen/deterministic.
8. Provider activation не зависит от reflection declaration order.
9. Interpreter/CIL единообразно классифицируют conversion failures.
10. Set-like container сохраняет uniqueness.
11. Legacy removal разрешён только после измеримой parity и migration gate.
12. Typed overlay не проходит через text/parser round-trip.
13. Internal verifier faults не маскируются под пользовательскую validation error.
