# Performance model

## Compiler-first summary

UniversalToolchain.Wist is compiler-first and interpreter-supported.

The main performance path is compiled typed execution: compile selected Wist formula/rule code once, then invoke the typed
compiled function many times. The interpreter exists for diagnostics, debugging, fallback, semantic parity, and
module/backend development.

## Cold path vs hot path

Cold path:

```text
source -> parse -> bind -> runtime selection -> compile/execute
```

Hot path:

```text
compiled typed function -> Invoke(arg0, arg1, ...)
```

The cold path pays for source handling and runtime selection. The hot path starts from an already compiled typed function.

## Use typed `Compile` for shipped-preset hot paths

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateRestrictedArithmetic();

var formula = wist.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.CompiledDelegate(100.0, 5.0);
```

Use this shape when the same formula is invoked repeatedly through a shipped `WistEngine` preset. Compile once, keep the
returned program, and call `CompiledDelegate` from the hot path.

For custom `.wistdialect` files, use [Custom Dialect Fast Invocation](/build-dsls/custom-dialect-fast-invocation). The
current custom-dialect path compiles through `WistRuntimeFacade.TryCompile` and reuses compiled artifacts or the
low-level CIL `DynamicMethod` output.

## Use Evaluate for one-off execution

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateRestrictedArithmetic();

double result = wist.Evaluate<double>(
    "price * 0.9 + fee",
    new
    {
        price = 100.0,
        fee = 5.0
    });
```

`Evaluate` is useful for one-off execution, tests, validation/admin UIs, and onboarding. It is not the primary runtime
throughput API.

## What the performance claim means

The performance claim belongs to compiled typed CIL-backed invocation.

It does not apply to:

- `Evaluate`;
- compile time;
- every possible dialect, module combination, or backend;
- interpreter execution;
- convenience paths that map arguments from anonymous objects or dictionaries.

Benchmarks should separate cold compile cost from hot invocation cost and include the .NET SDK, OS, CPU, command, commit,
and raw artifacts.

## What not to benchmark

Do not benchmark Evaluate inside a tight loop when evaluating runtime throughput.

Benchmark compiled `Invoke` for hot-path throughput. Benchmark `Evaluate` only when measuring convenience one-off
execution.

## Interpreter role

The interpreter is still part of the product, but it is not the performance story. Use it for:

- diagnostics;
- debugging;
- fallback;
- semantic parity checks;
- module/backend development;
- educational inspection.

Interpreter behavior should stay semantically aligned with compiler behavior for supported shapes, but optimized CIL
invocation is the performance-oriented path.

## Security note

Fast compiled execution is not sandboxing.

Restricted dialects and presets limit selected language/runtime surface. They do not create a hardened sandbox for
hostile or arbitrary user-supplied input. For that scenario, isolate execution outside the runtime with appropriate
process, environment, permission, and resource controls.
