# Pricing Restricted Dialect

## Allowed

External pricing inputs can be provided by the host:

```text
price * 0.9 + fee
```

```text
(price + fee) * 0.95
```

```text
price - discount
```

Standalone smoke execution uses literal values because it has no host-provided bindings:

```text
100.0 * 0.9 + 5.0
```

## Not allowed

This dialect is not for general programs.

It should not include:

- loops
- labels
- conditions
- local variables
- comments
- C# interop
- general statement syntax

## Why this exists

Pricing logic needs a narrow language with limited capabilities.
