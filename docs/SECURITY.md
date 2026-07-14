# Security Policy

## Reporting a vulnerability

Please report security vulnerabilities privately by opening a GitHub Security Advisory for this repository (preferred)
or by contacting the maintainer directly.

Do **not** disclose vulnerabilities publicly before a fix is available and maintainers have completed triage.

## Execution trust model

This repository includes runtime execution facilities (`Wistc run`, REPL, and programmatic execution hosts).

Dialect examples can constrain composition (for example, arithmetic-only interpreter profiles), but those constraints
must **not** be interpreted as fully hardened sandbox guarantees unless explicit code-level isolation controls are added
and verified.

Treat untrusted script execution as high risk. If you must run untrusted input, isolate it at the process/environment
level and disable unsafe capabilities by composition.

Internal targeted runtime activation (exact loading of selected component/registrar types) is an implementation detail, not a sandbox boundary. Constrained dialect composition is still not equivalent to hardened sandboxing. Do not treat "exact activation" as a security guarantee for untrusted code execution.

## Supported versions

This repository is currently developed on the default development branch and targets .NET 10 (`net10.0`). Security fixes
are expected to be applied there.

## Preview.3 host controls

The public facade now applies two bounded controls before composition or execution:

- `WistEngineOptions.AllowedAssemblies` is the explicit host CLR type/method allowlist. Only the shipped `BasicStdLib` assembly is added by the runtime; selected dialect implementation assemblies are not exposed. Runtime discovery does not inspect `AppDomain.CurrentDomain`, `AppContext.BaseDirectory`, or recursively load DLLs from disk.
- `WistEngineOptions.ResourceLimits` limits UTF-16 source length and external parameter count. Limit failures use stable structured diagnostic codes (`UTC-WIST-001` and `UTC-WIST-002`).

These are defense-in-depth and denial-of-service preflight checks, not a sandbox. They do not interrupt a long-running compiled delegate, constrain heap allocation, isolate native calls, or revoke capabilities after a host deliberately adds an assembly. For arbitrary untrusted authors, execute in a separate constrained process/container and enforce wall-clock, CPU, memory, filesystem, network, and identity limits outside Wist.

The name `CreateRestrictedArithmetic` describes the actual guarantee. Ambiguous “safe” or “trusted” aliases are intentionally not part of the public API because they could be misread as process-isolation guarantees.
