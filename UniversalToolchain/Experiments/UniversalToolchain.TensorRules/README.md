# TensorRules second-language package

TensorRules is a non-packable research language implemented only through the public language-authoring SDK. It does not reference the selected compiler implementation or compiler-internal projects.

The route is `source -> parser -> shape/type verification -> policy-controlled optimizer pass -> interpreter backend`.

Protocol/schema v2 preserves the historical v1 set of 12 cases (two valid, two initially invalid, eight fault operators) and adds two matched demand-baseline cases. Five policies produce 70 observations. The demand policy rejects the queried case but misses the matched unqueried case; selective obligation discharge and always-verify reject both. P2 and P3 must agree on every case, while P3 additionally verifies clean boundaries.

This package is model-authored and must be described as a **second language package**, not as an independently authored language.
