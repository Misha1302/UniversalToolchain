# Adversarial review 3 — submission, anonymity, and layout

Review type: model-authored adversarial pass.
Scope: official CGO 2027 constraints, anonymous paper, rendered PDF, archival artifact, anonymous supplementary package, and submission metadata boundaries.

## Checks performed

- Standard Research Paper target; anonymous ACM SIGPLAN review format;
- Letter page size, page and line numbers;
- nine-page PDF including references, below the stricter repository gate of eleven total pages;
- no undefined citations/references or overfull horizontal boxes;
- embedded fonts and black-and-white-readable tables/figures;
- visual inspection of all nine rendered pages, including lifecycle diagram and ablation tables;
- paper text scan for author names, repository identity, URLs, and local paths;
- separation of the non-anonymous archival artifact from the uploadable anonymous supplement;
- clean quick/full supplement replay and deterministic archive comparison;
- supplement scan for names, handles, public branding, GitHub URLs, exact Git revisions, local paths, credentials, and build caches.

## Findings and resolution

1. **The paper README still stated five pages.** It now states nine.
2. **The artifact README described the old 30-case/four-policy study.** It now describes the 32-case five-policy study and the matched demand pair.
3. **The archival source bundle was not appropriate for double-blind upload.** A separate neutral supplement now contains only selected raw evidence, analyzers, protocols, and anonymous paper source.
4. **Supplement reproducibility and anonymity were previously separate aspirations.** They are now mechanically coupled: the same manifest-covered package must pass both the anonymity scanner and raw-data table regeneration.

## Residual submission risks

The official rules and deadline must be rechecked immediately before actual submission. The public archival artifact must not be uploaded during double-blind review. External corpus and pinned-machine results must not be added to the abstract or conclusions without new evidence and a full claim audit.

## Verdict

`PASS_PENDING_FINAL_PROVIDER_RECEIPTS`: local visual, anonymity, reproducibility, and format checks pass. Final status is upgraded only after the clean exact-head CI and provider artifact digests are recorded.
