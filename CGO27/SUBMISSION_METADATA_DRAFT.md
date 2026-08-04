# CGO 2027 submission metadata draft

Status: draft only; not a submission receipt.

- Track/type: Standard Research Paper.
- Title: **Obligation-Guided Reverification for Composed Compiler Pipelines**.
- Authors: intentionally omitted from the branch-backed anonymous draft.
- Keywords: compiler verification; composed compilers; analysis invalidation; semantic obligations; intermediate representation; contracts.
- CCS primary: Software and its engineering — Compilers.
- CCS secondary: Software and its engineering — Software verification and validation.
- Main PDF: anonymous Letter PDF built from `CGO27/paper/main.tex`.
- Supplement for review: `cgo27-anonymous-supplement.tar.gz` only.
- Do not upload during double-blind review: the non-anonymous archival source artifact.

## Abstract

A composed compiler can preserve generic IR structure while violating a semantic relation introduced by its selected language, optimizer, or backend. Structural verification cannot check an absent selected-system relation, while lazy analysis invalidation only recomputes a fact when some downstream consumer asks for it. We present obligation-guided reverification: transformations declare required, produced, preserved, and invalidated facts; each invalidation creates a named obligation with a canonical verifier owner and first eligible boundary; and a fail-closed scheduler must discharge every due obligation before the artifact crosses that boundary. Under explicit effect-completeness, unique-route, scheduler, and verifier-soundness assumptions, selective policy P2 cannot cross a modeled boundary with a due false or undischarged considered fact.

We implement the model in a composed .NET compiler and compare structural-only, passive-invalidation, demand-recomputation, selective, and always-verify policies. A matched counterexample shows the central distinction: demand recomputation rejects an invalidated fact when queried but misses the otherwise identical unqueried fault, whereas P2 rejects both at the declared boundary. In a 32-program source-to-result study with 320 fresh-process records, P2 and always verification agree on all classifications and first detection boundaries; both reject seven targeted faults before backend execution, while demand recomputation catches only the explicitly queried demand fault. A second public-SDK language reproduces the queried/unqueried distinction. The corpora are study-authored, external independence remains blocked, and we make no whole-compilation speedup claim.

## Submission-time checklist

- recheck the official deadline, page rule, double-blind policy, and supplementary-material instructions;
- add the author list only in the submission system, not to the anonymous PDF or supplement;
- confirm every conflict/domain entry in the submission system manually;
- upload only provider-verified files whose SHA-256 values appear in the final readiness receipt;
- do not assert external independence or whole-compilation speedup.
