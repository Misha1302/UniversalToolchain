# Матрица совместимости generic Wist LanguagePack и shipped Wist

**Состояние на 2026-07-25:** `Wist subset alpha`, не полная замена legacy Wist dialect runtime.

Полная замена запрещена, пока каждый shipped preset не имеет статуса `Equivalent` либо `EquivalentWithKnownDifferences`, executable equivalence test и закрытый migration gate. Машиночитаемый источник: `WIST_PARITY_MATRIX.json`.

Статусы: `Missing`, `Partial`, `Equivalent`, `EquivalentWithKnownDifferences`, `Deprecated`, `Removed`.

## Backend-ы

| Legacy alias | Typed backend | Статус | Проверка / отличие |
|---|---|---|---|
| `interpreter` | `interpreter` | `Equivalent` | ConditionsAggregate_ProvidesControlFlowBooleanLogicAndComparisonsOnBothBackends; ManagedCallContractRegressionTests |
| `cil (legacy alias: compiler)` | `cil` | `EquivalentWithKnownDifferences` | ConditionsAggregate_ProvidesControlFlowBooleanLogicAndComparisonsOnBothBackends; ManagedCallContractRegressionTests; Public typed ID is cil; legacy runtime also accepts compiler as a compatibility alias. |

## Modules

| Legacy alias | Typed feature | Interpreter | CIL | Статус |
|---|---|---|---|---|
| `Arithmetic` | `wist.arithmetic` | Verified | Verified | `Equivalent` |
| `BooleanConditions` | `wist.boolean-logic` | Verified | Verified | `Equivalent` |
| `Comments` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `ComparisonConditions` | `wist.comparisons` | Verified | Verified | `Equivalent` |
| `Conditions` | `wist.conditional-control-flow / wist.conditions aggregate` | Verified | Verified | `Equivalent` |
| `CSharpInterop` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Equality` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `FunctionCalls` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Identifier` | `wist.identifiers` | Verified | Verified | `Equivalent` |
| `InternalPreprocessorLexemes` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Labels` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Loops` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `NativeTypes` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Numbers` | `wist.numbers` | Verified | Verified | `Equivalent` |
| `ParametersSetter` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `SafeMathFunctions` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Scopes` | `wist.scopes` | Verified | Verified | `Equivalent` |
| `SemicolonAsNewLine` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `TextualAddition` | `—` | LegacyOnly | LegacyOnly | `Missing` |
| `Variables` | `wist.variables` | Verified | Verified | `Equivalent` |
| `Whitespaces` | `wist.whitespaces` | Verified | Verified | `Equivalent` |

## Optimizers

| Legacy alias | Typed feature | Статус | Причина |
|---|---|---|---|
| `ArithmeticOptimization` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |
| `BooleanOptimization` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |
| `ComparisonIntrinsicOptimization` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |
| `EGraphOptimization` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |
| `NativeCilOptimization` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |
| `NativeTypesOptimization` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |
| `Ssa` | — | `Missing` | Generic Wist LanguagePack currently selects no optimizer contributions. |

## Shipped presets

| Preset | Typed coverage | Статус | Executable equivalence / gap |
|---|---|---|---|
| `minimal-arithmetic` | `wist.arithmetic` | `Equivalent` | MinimalArithmeticPreset_GenericPack_HasExecutableEquivalence |
| `full-default` | `wist.arithmetic`, `wist.boolean-logic`, `wist.comparisons`, `wist.conditional-control-flow`, `wist.identifiers`, `wist.variables` | `Partial` | Comments, CSharpInterop, Equality, Labels, Loops, SemicolonAsNewLine and both optimizers are not represented by the generic pack. |
| `full-default-native` | `wist.boolean-logic`, `wist.comparisons`, `wist.conditional-control-flow`, `wist.identifiers`, `wist.variables` | `Partial` | NativeTypes, interop, structural modules and all optimizers are outside the generic pack. |
| `minimal-arithmetic-native` | `wist.numbers` | `Partial` | NativeTypes and native optimizers are not represented by the generic pack. |
| `pricing-restricted` | `wist.identifiers`, `wist.variables` | `Partial` | NativeTypes, optimizer selection, exclusions and restricted-security policy are not represented as an equivalent generic preset. |
| `ssa` | `wist.identifiers`, `wist.variables` | `Partial` | SSA and native optimizer route are not represented by the generic pack. |
| `composition-restricted` | `wist.arithmetic`, `wist.boolean-logic`, `wist.comparisons`, `wist.conditional-control-flow` | `Partial` | Comments, Equality, exclusions, composition-restricted capability and security semantics are not equivalent. |

## Решение о миграции

- Generic pack можно использовать только как проверенный subset для перечисленных typed features.
- `minimal-arithmetic` имеет executable equivalence test.
- Остальные shipped presets остаются legacy-first; их удаление или автоматическая миграция запрещены.
- Optimizer parity отсутствует: generic pack не должен молча добавлять optimizer, которого нет в verified plan.
- Любое изменение статуса проверяется architecture test-ом и versioned deprecation gate.
