# TensorRules second-language package

TensorRules is a non-packable research language implemented only through the public UniversalToolchain language-authoring SDK. It does not reference Wist or compiler-internal projects.

The route is `source -> parser -> shape/type verification -> policy-controlled optimizer pass -> interpreter backend`.

The study contains 12 cases: two valid examples, two invalid examples and eight fault operators. Four policies produce 48 observations. `P2_SELECTIVE` and `P3_ALWAYS` must agree on every case; `P3_ALWAYS` additionally verifies clean boundaries while `P2_SELECTIVE` does not.

This package is model-authored and must be described as a **second language package**, not as an independently authored language.
