---
title: Single-owner slots
navigation: hidden
status: Internal maintainer architecture evidence for the slot-cardinality contract (UT-A007).
---

# Single-owner slots

## The law, and where it used to live

The project forbids a second parser, a second binder and a second runtime provider. A plan with two
front ends does not fail loudly — it resolves to whichever contribution the planner reached first, and the
result is a language whose meaning depends on traversal order.

Before this change the law existed in two weak places:

- `UTL2203` enforced exactly one owner for the **backend** slot, per enabled backend;
- every other slot relied on each contribution happening to declare `LanguageSlotMultiplicity.Single`.

`Many` is the constructor default. So an external package that said nothing about multiplicity got a
second binder, and the plan reported no diagnostic. Convention cannot enforce a law, and a rule that only
applies when the package remembers to state it is not a rule.

## What replaced it

`LanguageSlotTable` in `UniversalToolchain.FeatureSdk` declares the cardinality of a slot **once**, as a
property of the slot:

| Slot | Cardinality | Rationale |
|---|---|---|
| `frontend.parser` | single | two parsers make entry semantics depend on traversal order |
| `semantics.binding` | single | two binders produce two semantic programs nothing can choose between |
| `runtime.provider` | single | a plan is executed by one provider |
| everything else | many | undeclared slots keep whatever cardinality the package declares |

A violation is `UTL2101`, and the message carries the declared rationale — the author is told that this is
a project law rather than a preference some package expressed.

## What the table deliberately does not contain

This is the part that only measurement could settle. Three plausible entries are **wrong**:

- **`lowering` is not single-owner.** A measured `Syntax -> Air -> Executable` pipeline uses two
  `lowering` contributions and is the normal shape of a lowering chain. Declaring the slot single-owner
  would have forbidden a legitimate two-step lowering; the test
  `LoweringChainWithTwoOwners_IsStillAccepted` fails if anyone adds it.
- **`frontend.syntax` is not single-owner.** The `full-default` Wist preset selects **15** module
  contributions in that slot. It is a module extension point, not a stage.
- **`backends` is not single-owner in the table.** Two backends coexist by design (`interpreter` and
  `cil`). The correct rule is one owner *per backend capability*, already enforced by `UTL2203`, and the
  table cannot express a per-capability cardinality.

Two further behaviours that look like the same defect but are not:

- two transformation edges offering the same `Source -> Target` step with **different costs** is route
  search, and picks the cheap one deliberately;
- two same-contract passes with **equal order** is `UTL2208`, a separate and more specific rule.

## Scope of the check

Undeclared slots stay many-owner, and the package-level rule (`Multiplicity = Single` or a rejecting merge
policy) still fires with its own wording. A language author who invents a slot keeps full control; the
table only constrains the slots the project itself names.

`runtime.provider` is covered by `UTL2302` ("selection is ambiguous"), which reports before slot policies
run. It is in the table for documentation and for the day that ordering changes, and the coverage test
records that fact rather than pretending a separate fixture exists.
