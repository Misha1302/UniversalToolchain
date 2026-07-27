# Независимый повторный аудит релиз-кандидата Wist no-legacy

> Исторический snapshot до ремедиации 2026-07-27. Текущий статус и проверяемые результаты находятся в `REMEDIATION_REPORT_RU.md`; числа и release-оценки ниже не являются authority для исправленного дерева.


Дата: 2026-07-26

## Вердикт по исходному candidate

Исходный `Wist-no-legacy-2026-07-26.zip` отозван. Его нельзя внедрять: повторный аудит обнаружил релиз-блокирующие дефекты в runtime policy, parity evidence и installer verification.

## Подтверждённые дефекты и исправления

| ID | Дефект | Влияние | Исправление | Проверка |
|---|---|---|---|---|
| R-01 | Typed plan мог выбрать `CSharpInterop` при `AllowHostInterop=false` | Обход security policy | Policy валидирует выбранные runtime-модули, capability metadata и allowed assemblies | Negative test через `LanguageRuntime.Create` |
| R-02 | Прямой `ILanguageRuntimeProvider.CreateSession` пропускал policy validation | Обход R-01 через низкоуровневый публичный API | `CreateSession` повторно вызывает `ValidatePolicy` | Negative direct-provider test |
| R-03 | Full interop presets теряли `unsafe-interop` capability | Typed plan отличался от shipped dialect semantics | Capability зафиксирована в definitions и fail-safe materialization | Preset metadata regression |
| R-04 | `Create("FULL-DEFAULT")` сохранял пользовательский регистр в ID | Недетерминированные identity/hash/cache keys | Используется канонический ключ preset registry | Canonical identity regression |
| R-05 | Matrix выдавала `ProgramStructure` за несуществующую typed feature | Ложный green gate F-10 | Отдельная `infrastructureModules`; feature IDs сверяются с package descriptor | Architecture mapping test |
| R-06 | `Equivalent` применялся к entries без behavioral proof | Завышенная release claim | Разделены `SelectionEquivalent`, `InfrastructureEquivalent`, `ExecutableEquivalent` | Matrix schema/status gate |
| R-07 | Preset gate только создавал sessions | Не ловил runtime semantic drift | Все 7 presets исполняются typed/shipped на каждом backend | Executable parity corpus |
| R-08 | Installer сохранял старые `bin/obj` и делал incremental verification | Возможна проверка старых DLL вместо нового source | Старые outputs удаляются; build выполняется `--no-incremental` | Installer smoke с stale output |
| R-09 | Installer мог перезаписать произвольный чистый checkout | Неконтролируемая полная замена | Exact baseline gate; divergence требует явного override | Negative installer smoke |
| R-10 | Ошибка `git commit` не делала установку неуспешной | Ложное сообщение `ГОТОВО` | Commit failure возвращает exit 2 | Installer control-flow smoke |
| R-11 | Capability directives зависели от dictionary enumeration | Потенциально недетерминированная materialization | Ordinal sort перед созданием directives | Deterministic source review + tests |
| R-12 | NuGet metadata называла пакет legacy adapter-ом | Ложное публичное описание архитектуры | Описание синхронизировано с реальным owner boundary | Package inspection |

## Проверенные границы

- production runtime и package code;
- typed definitions/provider/materialization;
- parity matrix и architecture gate;
- все 1508 test cases;
- обе solution и оба samples;
- 9 NuGet packages, facade surface и два clean consumers;
- установочный сценарий, baseline guard и stale-output cleanup;
- source-only archive structure и manifest.

## Остаточные ограничения утверждений

- typed Wist не является второй независимой реализацией execution engine: он использует канонический Wist dialect composer/runtime;
- полная executable parity доказана для shipped presets на репрезентативном corpus;
- отдельные module/optimizer entries имеют selection equivalence, а не универсальное доказательство всей семантики вне preset compositions.
