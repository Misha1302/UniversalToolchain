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

## Use CompileFunc for hot paths

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

var formula = wist.CompileFunc<double, double, double>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = formula.Invoke(100.0, 5.0);
```

Use this shape when the same formula is invoked repeatedly. Compile once, keep the returned function, and call `Invoke`
from the hot path.

## Use Evaluate for one-off execution

```csharp
using UniversalToolchain.Wist;

using var wist = WistEngine.CreateSafeFormulas();

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
arbitrary untrusted code. Untrusted code requires process/environment isolation, restricted OS permissions, and resource
limits outside the runtime.
