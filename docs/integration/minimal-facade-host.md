# Minimal Facade Host

The smallest recommended host path is the Wist runtime facade. It keeps service wiring and dialect host construction out
of first-contact application code.

## Safe default host

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

`CreateDefault()` uses the built-in safe default profile. It supports the current pricing-style formula path and rejects
unsafe interop instead of enabling it by default.

## Trusted default host

```csharp
using UniversalToolchain.Dialects.Wist;

using var wist = WistRuntimeFacadeBuilder
    .CreateTrustedDefault()
    .Build();
```

`CreateTrustedDefault()` is an explicit trusted opt-in. Use it only when the host intentionally wants the broader Wist
profile with unsafe interop enabled.

## Restricted dialect file override

```csharp
using UniversalToolchain.Dialects.Wist;

using var wist = WistRuntimeFacadeBuilder
    .CreateDefault()
    .WithDialectFile("UniversalToolchain/Dialects/examples/wist/pricing-restricted/dialect.wistdialect")
    .Build();

var attempt = wist.TryCompile(
    """
    let discount = 0.9
    price * discount + fee
    """,
    new Dictionary<string, Type>
    {
        ["price"] = typeof(double),
        ["fee"] = typeof(double)
    },
    mode: "interpreter");

Console.WriteLine(attempt.IsSuccess);
```

`WithDialectFile(...)` replaces the built-in facade profile with the supplied `.wistdialect` file. The
`pricing-restricted` example intentionally accepts the simple pricing formula shape and rejects statement-style binding
syntax.

## Trust boundary

The safe default and restricted dialect files constrain runtime composition. They are not hardened sandboxes. Use
separate process, filesystem, network, and resource isolation for untrusted execution.
