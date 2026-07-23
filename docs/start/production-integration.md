---
title: Wist Production Integration
description: Validate, approve, compile, activate and roll back Wist formulas without losing the last-known-good program.
audience: wist-application-developer
status: current-alpha-guidance
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Wist production integration

This guide describes the host-owned lifecycle around `UniversalToolchain.Wist`. The framework validates and compiles formulas; the application owns versioning, authorization, approval, persistence, activation, rollback, observability and process isolation.

## Recommended lifecycle

```text
candidate source
  -> preflight limits
  -> Validate / TryCompile
  -> store draft + diagnostics
  -> human or policy approval
  -> create replacement engine/program
  -> atomic activation
  -> retain last-known-good version
  -> observe failures
  -> rollback or replace
```

Do not overwrite the active rule merely because a new draft was submitted.

## Separate draft, approved and active states

A production host normally needs at least three records:

| State | Meaning |
|---|---|
| draft | user-editable source that may be invalid |
| approved | source/version authorized for activation |
| active | exact compiled program currently serving traffic |

Store a content hash and application-owned version ID with every transition. Do not use `WistProgramMetadata.SourceText` as the only durable source record; metadata intentionally retains the full formula in process memory but does not provide persistence or audit semantics.

## Compile without replacing the active program

Use `TryCompile<TDelegate>` for expected authoring failures:

```csharp
using UniversalToolchain.Wist;

static WistCompileResult<Func<double, double, double>> TryBuild(
    WistEngine engine,
    string source)
{
    return engine.TryCompile<Func<double, double, double>>(
        source,
        "amount",
        "risk");
}
```

On failure, return structured diagnostics to the authoring UI and keep the last-known-good program active:

```csharp
var result = TryBuild(candidateEngine, candidateSource);
if (!result.IsSuccess)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        logger.LogWarning(
            "Wist rule rejected: {Code} at {Stage}",
            diagnostic.Code,
            diagnostic.Stage);
    }

    return RuleUpdateResult.Rejected(result.Diagnostics);
}
```

Avoid logging the formula, parameter values, `Exception.Message` or `WistProgramMetadata.SourceText` by default. Diagnostic messages may include lower-level text; treat them as potentially sensitive unless the deployment has a reviewed redaction policy.

## Activate as one owned snapshot

Keep the engine and compiled program in one application-owned object. The public alpha does not define a separate disposable program object, and the engine owns runtime resources used to create it. Keep the engine alive for the lifetime of the active snapshot.

```csharp
sealed class RuleSnapshot : IDisposable
{
    public RuleSnapshot(
        string version,
        WistEngine engine,
        WistProgram<Func<double, double, double>> program)
    {
        Version = version;
        Engine = engine;
        Program = program;
    }

    public string Version { get; }
    public WistEngine Engine { get; }
    public WistProgram<Func<double, double, double>> Program { get; }

    public double Execute(double amount, double risk) =>
        Program.CompiledDelegate(amount, risk);

    public void Dispose() => Engine.Dispose();
}
```

The host must synchronize replacement and disposal. A simple service can use a reader/writer lock so that the old engine is not disposed while a caller is executing it:

```csharp
sealed class RuleService : IDisposable
{
    private readonly ReaderWriterLockSlim _gate = new();
    private RuleSnapshot _active;

    public RuleService(RuleSnapshot initial) => _active = initial;

    public double Execute(double amount, double risk)
    {
        _gate.EnterReadLock();
        try
        {
            return _active.Execute(amount, risk);
        }
        finally
        {
            _gate.ExitReadLock();
        }
    }

    public void Replace(RuleSnapshot replacement)
    {
        RuleSnapshot previous;

        _gate.EnterWriteLock();
        try
        {
            previous = _active;
            _active = replacement;
        }
        finally
        {
            _gate.ExitWriteLock();
        }

        previous.Dispose();
    }

    public void Dispose()
    {
        _gate.EnterWriteLock();
        try
        {
            _active.Dispose();
        }
        finally
        {
            _gate.ExitWriteLock();
            _gate.Dispose();
        }
    }
}
```

This is an application pattern, not a framework-provided hot-reload manager. For high-throughput systems, use an ownership mechanism that waits for in-flight readers without serializing every invocation.

## Build a replacement snapshot

```csharp
static RuleUpdateResult PrepareReplacement(
    string version,
    string source,
    out RuleSnapshot? snapshot)
{
    var engine = WistEngine.CreateRestrictedArithmetic();
    var result = engine.TryCompile<Func<double, double, double>>(
        source,
        "amount",
        "risk");

    if (!result.IsSuccess || result.Program is null)
    {
        engine.Dispose();
        snapshot = null;
        return RuleUpdateResult.Rejected(result.Diagnostics);
    }

    snapshot = new RuleSnapshot(version, engine, result.Program);
    return RuleUpdateResult.Prepared(version);
}
```

Only call `Replace` after authorization and any domain-specific acceptance checks pass.

## Domain acceptance is separate from compilation

Compilation proves that the formula is syntactically and operationally supported by the selected Wist profile. It does not prove that the formula is correct for the business domain.

Before activation, evaluate an application-owned corpus:

- normal examples;
- boundary values;
- invalid or missing external inputs;
- monotonicity or range constraints where required;
- parity with the previous version for unchanged cases;
- explicitly approved behavior changes.

Reject `NaN`, infinity, overflow-sensitive or out-of-domain results according to the host's policy.

## Rollback

Keep the previous approved source and version identity until the new version has passed the deployment observation window. Rollback should compile and activate the known source through the same controlled path; do not deserialize undocumented backend artifacts.

A rollback record should contain:

- application rule ID;
- version ID and source hash;
- approval identity/time;
- diagnostic summary;
- activation and rollback reason;
- framework/package version and selected preset/backend.

## Caching

Cache by a key that includes all semantics affecting compilation:

```text
formula hash
+ ordered parameter names and CLR types
+ Wist package version
+ preset or custom dialect identity
+ backend/options snapshot
+ optimization policy
```

Do not reuse a compiled delegate under a different parameter order or options snapshot.

## Concurrency boundary

The public alpha does not claim that arbitrary `WistEngine` operations, custom interop or custom dialect components are universally thread-safe. Use a single-writer update path, synchronize engine replacement/disposal and validate concurrency for the exact profile you deploy.

A restricted compiled delegate containing only deterministic arithmetic is easier to invoke concurrently than a profile with host interop or mutable runtime components, but the host remains responsible for proving that property for its selected surface.

## Failure handling

| Failure | Host action |
|---|---|
| validation/compilation diagnostic | reject draft; keep active version |
| domain acceptance failure | reject approval; keep active version |
| activation infrastructure failure | dispose replacement; keep active version |
| runtime exception | record version/code without sensitive values; apply domain fallback or rollback policy |
| process/resource limit breach | terminate or isolate at the process/container boundary |

## Related references

- [Diagnostics](/reference/diagnostics)
- [Lifecycle, concurrency and privacy](/reference/lifecycle-concurrency-privacy)
- [Security](/SECURITY)
- [Performance model](/reference/performance-model)
- [Use-case recipes](/start/use-case-recipes)
