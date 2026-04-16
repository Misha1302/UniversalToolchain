# Minimal façade host

## Safe default example

```csharp
using UniversalToolchain.Dialects.Wist;

using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .Build();

var result = wist.Run(
    "price * 0.9 + fee",
    new Dictionary<string, object?>
    {
        ["price"] = 100.0d,
        ["fee"] = 5.0d
    },
    mode: "compiler");

Console.WriteLine(result);
```

- `CreateDefault()` is the safe first-contact profile.
- Compiler and interpreter modes are both available.
- C# interop is not enabled by default.

## Trusted default example

```csharp
using var wist = WistRuntimeFacadeBuilder
    .CreateTrustedDefault()
    .Build();
```

Use `CreateTrustedDefault()` only for explicitly trusted scenarios. Do not use it for untrusted input.

## Dialect file example

```csharp
using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithDialectFile("pricing-restricted/dialect.wistdialect")
    .Build();
```

Use this when the runtime surface must be explicitly restricted. The dialect file overrides built-in profile selection.

## Warnings

- Safe composition is not hardened sandboxing.
- Untrusted execution still requires process or environment isolation.
- Interop-enabled trusted paths should be treated as high trust only.
