# What is stable in this preview

UniversalToolchain.Wist `0.1.0-preview.4` is a public preview. It is stable enough to try in controlled evaluation
and prototype scenarios, but it is not a stable 1.0 contract.

## Stable enough to try

- `WistEngine`;
- `WistEngine.CreateRestrictedArithmetic`;
- `WistEngine.CreateFullNativePreview`;
- compatibility aliases `WistEngine.CreateSafeFormulas`, `WistEngine.CreateBusinessRules`, and `WistEngine.CreateTrusted`
  with their documented preview mappings;
- `Compile<TDelegate>` for typed delegate-backed hot-path formulas;
- `TryCompile<TDelegate>` for non-throwing typed delegate compilation;
- compatibility `CompileFunc` overloads for one, two, and three arguments;
- `Evaluate`;
- `Validate`;
- packaged Wist facade usage from a clean .NET project;
- `WistEngineOptions.Optimization.Ssa` as an opt-in experimental route selector;
- facade-owned SSA optimization reports on validation, `TryCompile`, and compiled program metadata;
- a reviewed facade API baseline in `UniversalToolchain.Wist/PublicAPI.Shipped.txt`.

## Preview / may change

- wider `CompileFunc` arities;
- additional signature-based compiled API shapes;
- object/session-based compiled artifacts;
- the detailed fields and trace vocabulary inside the experimental SSA report;
- low-level `UniversalToolchain.Ssa.*` APIs and extension-pack contracts;
- product-oriented preset names beyond their documented preview alias mappings;
- backend-agnostic compiled artifact API;
- third-party DSL authoring ergonomics;
- backend authoring contracts;
- module authoring templates;
- benchmark reporting format.

## Not promised

- hardened sandbox;
- stable generic framework API;
- universal near-C# speed for all modes;
- frictionless third-party DSL authoring.
