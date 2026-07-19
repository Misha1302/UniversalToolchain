# REBASE 2026 speaker preparation notes

Status: proposal.

These notes support delivery of the talk. They are not part of the submission abstract.

## Scope discipline

- Explain only the architecture needed for the running example.
- Do not use class diagrams or long interface inventories.
- Keep semantic parity as evidence for the architecture, not as a replacement for the main topic.
- Do not claim production deployment, hardened sandboxing, stable 1.0 compatibility, or general parity with handwritten C#.

## Demonstration sequence

1. Show the pricing formula and the three execution paths.
2. Show equal results.
3. Show rejection of the excluded statement-style feature.
4. Inspect the selected runtime plan.
5. Trace one operation from module contribution to typed CIL.
6. Show the fixed binding/local regression tests.

Do not intentionally break the current runtime during the talk. Use the preserved reproducer and the current passing regression tests.

## Performance evidence

Present the measurement model before any number:

- prepared invocation excludes parsing, dialect composition, and compilation;
- convenience evaluation includes public API overhead and is not a hot-path comparison;
- compilation benchmarks measure engine creation and formula preparation, not runtime throughput.

Show numeric results only after a fresh run records the exact commit, working-tree state, SDK/runtime, CPU, OS, comparison contract, and raw BenchmarkDotNet artifacts.

## Demo fallback

Prepare three levels:

- **Primary:** the live shared demo script with restore and build completed before the session;
- **Fallback:** a terminal recording of the same command and output;
- **Last resort:** screenshots, the committed expected output, and direct links to the focused regression tests.

Do not depend on conference Wi-Fi. Prepare the environment in advance and use `--no-restore --no-build` for live commands after the build is complete.

## Closing checks

Before presenting:

- confirm the repository commit used by the slides and demo;
- run the shared demo script;
- confirm all links in the reviewer page still resolve;
- check that the abstract, slides, and spoken claims use the same project boundaries;
- keep the closing sentence only once: “Extensible during construction. Specialized during execution. One semantics across supported runtimes.”
