# Adversarial review 1 — formal model and implementation alignment

Review type: model-authored adversarial pass.
Scope: `FORMAL_MODEL_AUDIT.md`, paper Sections 2--4, obligation contracts, scheduler, focused tests, and the strongest implemented alternative (`P1D`).

## Objective lock

The result is acceptable only if the paper's guarantee is the same conditional guarantee enforced by the implementation. It is not enough for the prose to sound stronger than invalidation or for the code merely to detect the current seeded faults.

## Strongest material alternative

The comparison baseline is demand-driven invalidation and recomputation, not structural checking alone. `P1D` therefore receives the same queried fault that the obligation policy receives, plus an otherwise identical unqueried case that it is allowed to defer.

## Findings and resolution

1. **Fact state was originally binary.** That allowed missing knowledge to look equivalent to invalidity. The implementation now distinguishes `valid`, `invalid`, and `unknown`; unknown demand is fail-closed.
2. **Invalidation originally did not create an owned deadline.** The implementation now creates a boundary-indexed `VerificationObligation` with canonical owner and first eligible boundary.
3. **A missing verifier route could silently leave no obligation.** This is now rejected by `UT-PIPELINE-EFFECT-006`.
4. **The earlier baseline was weaker than realistic demand recomputation.** `P1D` is executable and detects the queried case while deferring the unqueried twin.
5. **The theorem could be read as whole-compiler soundness.** The statement, assumptions, and threats now restrict it to declared facts, explicit boundaries, registered canonical routes, and verifier correctness.

## Adversarial counterexample check

- owner mismatch: rejected;
- missing route: rejected;
- unknown query: rejected;
- missed first eligible boundary: rejected;
- unqueried P1D twin: deferred by design, while P2/P3 reject it at the declared boundary;
- repeated occurrence and route-conflict mechanisms: each loses exactly its corresponding detection when ablated.

## Residual limitations

The proof is relative to declared contracts. It does not prove the verifier, frontend, backend, or undeclared state correct; it does not cover hidden mutations that never enter the contract system.

## Verdict

`PASS_BOUNDED`: theorem, assumptions, non-claims, code, and focused counterexamples are aligned. No unresolved theorem/code mismatch remains.
