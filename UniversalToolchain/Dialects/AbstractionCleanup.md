# Dialect abstraction cleanup (interface review)

## Removed

### `IDialectDefinition`

- **Why removed:** it exposed only `Name`, `Version`, and `BaseDialectName`, while real semantics live on concrete `DialectDefinition` (module/backend/intrinsic/optimizer/security/capability policies and order rules).
- **Issue:** callers using this interface would still need concrete-type knowledge for meaningful domain work, so the abstraction was misleading.
- **Result:** use the immutable concrete `DialectDefinition` directly.

### `IDialectPolicyValidator`

- **Why removed:** it had one method with one implementation and no distinct behavior variants.
- **Issue:** the interface added indirection but no practical extension seam.
- **Result:** keep `DialectPolicyValidator` as a concrete adapter around `DialectBuildPlanBuilder` diagnostics.

## Kept (and why)

### `IDialectDefinitionParser`

- Meaningful seam between parsing workflow and parser implementation.
- Used by `DialectInspectWorkflow` for explicit orchestration dependency.

### `IDialectBuildPlanBuilder` and `IDialectCompiledDialectBuildPlanBuilder`

- Distinct semantic contracts for two source models:
  - parsed syntax document,
  - framework-compiled directive slice.
- Keep orchestration layers decoupled from specific builder classes.

### `IDialectRuntimeCompositionResolver`

- Meaningful runtime composition seam consumed by workflows.
- Allows alternate resolution policies for runtime descriptor matching.

### `IDialectPolicyValidationRule`

- Real extensibility seam used by policy validator pipeline.
- Enables rule additions without central conditional growth.
