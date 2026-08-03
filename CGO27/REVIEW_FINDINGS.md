# CGO 2027 adversarial review findings

Status: `REMEDIATED_PENDING_EXACT_HEAD_PROVIDER_VALIDATION`.

This is a model-authored adversarial review of the implementation, experiment design, paper, and artifact. It is not an independent human peer review and does not satisfy the external-corpus requirement.

## Findings

| ID | Severity | Finding | Resolution / current state |
|---|---|---|---|
| F-01 | blocker | Research workflows checked out a synthetic pull-request merge revision while PR text described the branch head. Artifact names and `COMMIT` receipts could therefore certify a different tree from the claimed one. | All research workflows now resolve `pull_request.head.sha`, check out that exact revision, assert `git rev-parse HEAD`, and use the same SHA in artifact names and receipts. Clean source archives use a manifest-covered 40-hex `COMMIT` only when `.git` is absent. Provider confirmation is pending. |
| F-02 | blocker | `ObservedBytecodeEmission.SourceNode` was captured but ignored by `BytecodeVerifier`; source identity was documentation rather than an enforced contract. | Production verification now distinguishes undeclared producer from declared producer/wrong source node, emits `UT-BYTECODE-SOURCE-001`, and has regression coverage within the canonical 1,579-test contract. |
| F-03 | major | The original ablation report compared only P0/P1/P2/P3, so it could not attribute necessity to the eight individually claimed mechanisms. | A non-packable executable now runs eight minimal counterexamples and eight matched controls. Full protocol must detect 8/8 with predeclared codes; each single-mechanism ablation must lose exactly its corresponding detection; control false positives must remain 0/8. |
| F-04 | major | Paper ablation tables were manually transcribed, allowing the article to diverge from raw evidence. | A deterministic renderer produces compact LaTeX from validated schema-v3 ablation JSON. The ablation runner byte-compares regenerated tables with committed anonymous paper sources. |
| F-05 | major | Tightening provenance to `git rev-parse HEAD` broke clean-unpack reproduction because `git archive` intentionally omits `.git`. | Repository executions prefer real Git HEAD. Clean artifacts first verify `MANIFEST.sha256`, validate their 40-hex `COMMIT`, export it to every runner, and require all boundary/E2E/TensorRules/mechanism receipts to agree. |
| F-06 | moderate | End-to-end case P07 is a pre-existing mixed parameter/literal runtime assertion under every policy. Counting it as a control or protocol fault would bias results. | P07 remains in the corpus as a separately validated baseline runtime failure. The evaluated valid-control count is 24, not 25. The product defect itself is not fixed by this research PR. |
| F-07 | blocker for external validity | All primary, challenge, System W, TensorRules, and mechanism-ablation faults are author/model designed and may align with implementation structure. | Deterministic external author/freeze/import tooling exists, but no human-authored frozen corpus has been supplied. Claim remains `BLOCKED_EXTERNAL`. |
| F-08 | blocker for performance | Shared CI timings and the observed 25% verifier-call reduction do not establish whole-compilation speedup. | The efficiency headline is forbidden. Decision-grade timing remains `BLOCKED_PINNED_MACHINE` until the frozen pinned-machine protocol is executed. |
| F-09 | moderate | A structural-validity counterexample was asserted informally, weakening the distinction between generic IR validity and selected-system validity. | The paper now states a capability counterexample with explicit assumptions and conclusion, while keeping general soundness and equivalence claims out of scope. |
| F-10 | moderate | The anonymous paper source artifact omitted evidence-generated table inputs and was not self-contained. | Paper workflow now includes `generated/` beside `sections/`; local preflight builds a five-page Letter PDF with embedded fonts and no overfull or unresolved-reference diagnostics. Exact-head provider confirmation is pending. |

## Rejection-oriented assessment

The strongest remaining rejection argument is external validity: two language packages and eight isolated mechanisms show concrete necessity witnesses inside one framework, but they do not establish natural-fault prevalence or transfer to independently authored extensions. The paper must continue to frame TensorRules as model-authored and the mechanism corpus as diagnostic counterexamples, not representative sampling.

The second strongest argument is performance relevance. The work currently supports correctness and fault-localization claims. It does not support a speedup claim, and the paper correctly treats the 25% call-count reduction as isolated work-count evidence and not equivalent to whole-compilation time.

The novelty position is narrower than verified compilation or general translation validation: the contribution is fail-closed scheduling of heterogeneous existing verifiers from selected-component contracts, with explicit canonical ownership and first eligible boundaries. The paper must not imply semantic soundness beyond the registered contracts and verifier implementations.

## Acceptance gate

The branch is not final until the exact current head passes:

- canonical `.NET CI` and architecture/documentation validation;
- Contract Experiment, Wist end-to-end, TensorRules, and schema-v3 ablations;
- anonymous paper preflight;
- deterministic dual artifact build and clean-unpack quick check;
- package compatibility, rollout, benchmark smoke, and published-package smoke.

Merge, package publication, and conference submission remain outside this review's authority.
