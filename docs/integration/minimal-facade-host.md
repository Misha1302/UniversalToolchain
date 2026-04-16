# Minimal Facade Host

These examples use the Wist facade project through a project reference.

## Safe Default

`CreateDefault()` is the safe first-contact profile. It uses the restricted built-in facade profile and does not enable
C# interop by default.

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
```

## Trusted Default

`CreateTrustedDefault()` is trusted-only. Do not use it for untrusted input.

```csharp
using UniversalToolchain.Dialects.Wist;

using var wist = WistRuntimeFacadeBuilder
    .CreateTrustedDefault()
    .Build();

var result = wist.Run(
    "price * 0.9 + fee",
    new Dictionary<string, object?>
    {
        ["price"] = 100.0d,
        ["fee"] = 5.0d
    },
    mode: "compiler");
```

## Dialect File Override

Use `WithDialectFile(...)` when the host should provide an explicit dialect file instead of the built-in facade profile.

```csharp
using UniversalToolchain.Dialects.Wist;

using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithDialectFile("UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect")
    .Build();

var attempt = wist.TryCompile(
    "let discount = 0.9\nprice * discount + fee",
    new Dictionary<string, Type>
    {
        ["price"] = typeof(double),
        ["fee"] = typeof(double)
    },
    mode: "interpreter");
```

## Warnings

Safe composition is not hardened sandboxing.

Untrusted execution still requires process and environment isolation.
