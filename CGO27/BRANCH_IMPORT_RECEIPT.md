# CGO 2027 branch import receipt

Date: 2026-08-03.

## Authority chain

- Baseline branch head: `7840550ddbc8eb3762bd60babde3427eab02ab48`.
- Locally verified hardening tree was transported as a gzip-compressed tar payload.
- Payload SHA-256: `5311f090ba5b3f3c86f54517aaf1d23d28a3fafe00a67c6462d06684dfa10258`.
- GitHub Actions verified the payload checksum before extraction.
- Content commit: `9d99a1011f14cedc56b43015968a2511cc20dbfb`.
- Exact-head workspace workflow update: `4a7ed302eacc8839ff03f456a8a8548823099e67`.
- One-time payload workflow and all transport chunks were removed before this receipt.

## Locally completed validation before transport

- Focused obligation/scheduler tests: 23 passed, 0 failed, 0 skipped.
- Native arithmetic module regression suite: 21 passed, 0 failed, 0 skipped.
- Restricted dialect regression suite: 5 passed, 0 failed, 0 skipped.
- Affected canonical assemblies: Core 546, Modules 305, Dialects 639 passed; zero failed/skipped.
- Canonical repository total after mechanical recount: 1,623 tests.
- Release builds with warnings as errors: zero warnings and zero errors.
- Boundary protocol v4, Wist E2E schema v3, and Language T schema v2 passed their local validators.
- Paper source built in anonymous ACM review mode on Letter paper; the intermediate PDF was eight pages including references.

These local results are provisional until the clean branch head is re-executed by exact-head CI and downloaded artifacts are inspected.
