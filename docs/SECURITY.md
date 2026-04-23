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
