# Testing Guidelines by Layer

This matrix defines expected testing boundaries for UniversalToolchain and dialect-level test suites.

| Layer | May know internal types? | May use reflection to private members? | Must compare backend behavior? |
|---|---|---|---|
| Contract | No. Use only public contracts and observable behavior. | No. | Yes, when the contract is backend-agnostic and promises parity. |
| Parity | No for framework internals; only public surface and deterministic outputs/diagnostics. | No. | Yes. Parity tests exist specifically to compare enabled backends. |
| Backend-specific | Yes, but only for that backend's own internal implementation boundaries. | Rarely, and only with explicit justification in test intent. | No cross-backend parity requirement; validate backend-local guarantees. |
| Internal | Yes. Internal collaborators and implementation details are test scope. | Allowed when no stable extension point exists and intent is explicitly documented. | Optional. Compare backends only if the internal change affects backend selection/execution semantics. |

## Additional Rules

- Prefer extension-point-level assertions before internal implementation assertions.
- Avoid brittle reflection-based assertions when deterministic public outcomes can be asserted.
- If behavior changes by backend are intentional, assert the difference explicitly instead of masking it as parity.
- Keep diagnostics assertions deterministic (code/message fragment), not environment-dependent full text.
