# Выполненный механизмный probe

**Authority: dated-review / executable research artifact, 2026-10-26.** Production source baseline: `f4d1dd1de2d2e140437613191008b9cdf2552605`. Это исследовательская диагностика четырёх заранее выбранных моделью случаев, не внешний blind corpus, не новый natural-defect discovery, не полная компиляция программы и не benchmark.

## Что исполняется

[DiagnosticProbe.csproj](DiagnosticProbe.csproj) линкует неизменённый TensorRules Program.cs и ссылается на реальные production projects. [DiagnosticProbe.cs](DiagnosticProbe.cs) вызывает actual TensorPolicyPass и отдельно production ModuleContractVerificationScheduler с тем же TensorVerifier; manual arm — прямая typed conditional для одного правила. Reflection используется только внутри исследовательской обвязки для доступа к существующим внутренним fixture builders. Публичный API и generic runtime не меняются.

Independent local oracle проверяет выбранные shape relations и согласованность output projection. Он независим от scheduling/checker logic в этом опыте, но написан моделью, использует известную локальную спецификацию и не проверяет полный язык. Oracle для отклонённых прямых policy cases хранит заранее известную invalidity fixture; matched arm проверяет изменённый artifact непосредственно.

## Наблюдение

| Case | Selective | Always | Typed manual selective | Локальный oracle |
|---|---|---|---|---|
| Clean control | accepted | accepted | accepted | valid |
| Объявленная inner-shape corruption | rejected | rejected | rejected | invalid |
| Та же corruption без invalidation | accepted | rejected | accepted | invalid |
| Порча непроверяемого output projection | accepted | accepted | accepted | invalid |

Полный [results.json](results.json): 8 direct policy records + 12 matched records + 2 scheduler-count observations. **Четыре случая; не 22 дефекта и не 22 независимых испытания.** Production selective и manual совпали на этих четырёх случаях. Это не theorem общей эквивалентности и не измерение total integration cost. Missing declaration находится вне intended-use premise, поэтому probe не доказывает bug штатного P3/default Wist.

## Повторение

Из корня checkout с .NET SDK 10 выполнить команды ниже. Путь результата должен быть новым, чтобы не затереть сохранённый результат; RepoRoot по умолчанию выводится из расположения проекта. Для архива/другого layout передать `-p:RepoRoot=/absolute/path/to/repo`.

```text
dotnet run --project internal-docs/research/heterogeneous-dsl-2026-10-26/journal-decision/probe/DiagnosticProbe.csproj -c Release -p:NuGetAudit=false -- /tmp/ut-independent-probe-results.json
python3 internal-docs/research/heterogeneous-dsl-2026-10-26/journal-decision/probe/verify_probe.py --replay /tmp/ut-independent-probe-results.json
```

Первый выполненный прогон использовал явный RepoRoot на отдельной Fedora worktree и SDK 10.0.111. Доставляемый csproj меняет только переносимый default RepoRoot и IsPackable=false; окончательный повтор и exact hashes указаны в [receipt.json](receipt.json) и [VERIFICATION](../VERIFICATION.md). Сам C# harness и исходные production файлы сохранены.

Runner не имеет scientific parity assertion, поэтому наблюдённое расхождение само не блокирует запись JSON. Он собирает результат в памяти до записи; durability при аварийном падении процесса здесь **не** реализована. Требование I-01 из [NEXT_SPEC](../NEXT_SPEC.md) остаётся будущей работой и включает отдельный retention test.

[verify_probe.py](verify_probe.py) проверяет целостность source/results и соответствие сохранённому фактическому outcome. Это integrity check, не новый статистический тест гипотезы и не замена independent oracle. Сохранённые runtime/compile timings отсутствуют намеренно: build+run duration не характеризует быстродействие UTS.
