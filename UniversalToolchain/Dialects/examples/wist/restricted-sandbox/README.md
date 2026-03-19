# Restricted sandbox dialect

This example demonstrates a deliberately constrained runtime.

Enabled:
- arithmetic-only execution through the `interpreter` backend
- `Scopes`, `Numbers`, `Arithmetic`, and `Whitespaces`

Disabled by omission:
- the compiler backend
- identifiers and variables
- conditions, labels, loops, and interop

`program.wist` succeeds because it only needs arithmetic.
`forbidden-program.wist` is expected to fail because variable declarations are not part of this dialect.
