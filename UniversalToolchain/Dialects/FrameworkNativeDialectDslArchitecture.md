# Framework-native execution path (Dialect Definition DSL)

This note describes the current framework-native path used by the Dialect Definition DSL.

## Pipeline path

1. Dialect source text enters the frontend DSL module(s).
2. Frontend compilation produces `DialectDefinitionSlice`.
3. Core semantic builder converts compiled slice into `DialectBuildPlan`.
4. Integration resolver maps the build plan to `DialectRuntimeComposition` using an explicit descriptor registry.
5. Optional apply-mode builder maps resolved composition to `DialectApplyDescription`.

## Why this is framework-native

The DSL is implemented through the existing UniversalToolchain extension points, not as a side parser disconnected from the toolchain architecture. Contributors can reason about dialect compilation using the same staging model used by the rest of the system.

## Deliberate boundaries

- Frontend compilation does not decide runtime activation policy.
- Semantic stage does not parse or resolve runtime descriptors.
- Runtime resolution does not mutate DI or force host execution changes.
- Apply mode is explicit and opt-in.

## Contributor guidance

If you add behavior:

- parser shape changes -> `Parsing`/`Frontend`
- semantic rule changes -> `Core`
- runtime descriptor mapping changes -> `Integration`
- host activation wiring -> a future integration step outside current v1 scope
