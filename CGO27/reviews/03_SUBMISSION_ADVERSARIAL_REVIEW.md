# Adversarial review 3 — submission, anonymity, and layout

Review type: model-authored adversarial pass.
Scope: official CGO 2027 constraints, anonymous paper, rendered PDF, archival artifact, anonymous supplementary package, and submission metadata boundaries.

## Checks performed

- Standard Research Paper target and anonymous ACM SIGPLAN review format;
- Letter page size, page numbers, and line numbers;
- ten-page PDF with references beginning on the final page, below the repository's stricter eleven-total-page gate;
- no undefined citations/references, no overfull horizontal boxes, and embedded fonts;
- visual inspection of all ten rendered pages, including lifecycle figures, historical accounting, ablation tables, strongest-alternative comparison, disclosure, and references;
- black-and-white readability and absence of clipping, overlap, or unreadably scaled table text;
- paper text scan for author names, repository identity, URLs, and local paths;
- visible anonymous Generative AI usage disclosure consistent with human accountability and non-author status;
- separation of the non-anonymous archival artifact from the uploadable anonymous supplement;
- clean quick/full supplement replay, deterministic archive comparison, manifest validation, and regenerated-table byte comparison;
- supplement scan for names, handles, public branding, GitHub URLs, exact Git revisions, local paths, credentials, and build caches.

## Findings and resolution

1. **The manuscript underused the allowed space and omitted a material feature comparison.** It now contains ten total pages with expanded implementation procedure, units/oracle methodology, complete historical accounting, and a strongest-alternative comparison table.
2. **Historical prose had inconsistent excluded/blocked counts.** It now consistently reports 24 candidates = 3 included + 11 excluded + 10 blocked, matching the frozen machine-readable accounting.
3. **The manuscript lacked an explicit Generative AI disclosure.** A neutral anonymous disclosure now states the assisted tasks, human responsibilities, and that tools are neither authors nor independent evaluators.
4. **The first expanded related-work table exceeded the text width by 8.8 pt.** Column spacing was reduced without scaling the font; rebuilt output has no overfull horizontal box and remains readable at print scale.
5. **The public archival source bundle is unsuitable for double-blind upload.** The separately built neutral supplement contains only selected raw evidence, analyzers, protocols, and anonymous paper source.

## Residual submission risks

- Official rules and deadline must be rechecked immediately before actual submission.
- Public history can permit motivated de-anonymization; the package removes avoidable direct identifiers but cannot erase public provenance.
- The non-anonymous archival artifact must not be uploaded during double-blind review.
- `BLOCKED_EXTERNAL`, `BLOCKED_PINNED_MACHINE`, and `BLOCKED_RESOURCE` must remain explicit; no abstract/conclusion upgrade is allowed without new evidence and a full claim audit.
- The review is model-authored, not an independent human review.

## Verdict

`PASS_WITH_DECLARED_NONCLAIMS`: no blocking scientific, format, layout, anonymity, or reproducibility finding remains inside the bounded submission claim. Final delivery still requires one exact-head provider receipt in which all required workflows and provider artifacts refer to the same revision. This verdict does not authorize merge, publication, or submission.
