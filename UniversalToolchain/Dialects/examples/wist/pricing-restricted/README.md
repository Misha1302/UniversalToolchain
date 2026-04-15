# Pricing Restricted Dialect

## Allowed

```text
price * 0.9 + fee
```

```text
(price + fee) * 0.95
```

```text
price - discount
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
