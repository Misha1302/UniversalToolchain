# Research → decision → Codex handoff contract

**Template status: NOT YET EVIDENCED** for any Claude team not actually attached to an experimental result. The researcher may update a hypothesis ranking, falsifier or candidate disposition; cannot promote code scope or declare `PASS` alone. No autonomous merge, package publish or API change follows from this template.

## Required handoff artifact

```yaml
handoff_schema: 1
hypothesis_id: E1              # one of E1..E8 / explicit hypothesis
frozen_source:
  repository: Misha1302/UniversalToolchain
  commit_sha: REQUIRED_EXACT_SHA
  package_digests: REQUIRED_IF_EXECUTED
  dotnet_sdk_os: REQUIRED_IF_EXECUTED
  linked_sources: []           # permanent URLs and repository paths
research:
  state: NOT_YET_EVIDENCED     # NOT_RUN | IN_PROGRESS | PASS | FAIL | BLOCKED
  competing_hypotheses: [A_direct_interfaces, B_operation_interfaces, C_scoped_facts]
  prior_registration: REQUIRED_BEFORE_EXPERIMENT
  independent_controls: []
  raw_data_paths: []
  oracle_implementation_and_independence: REQUIRED
  positive_negative_mutation_replays: []
  independent_critic_findings: []
  limits_and_counterexamples: []
  measured_total_costs: {}
  disposition_proposal: RESEARCH  # never conflated with approval
promotion:
  decision_id: ADR-01
  decision_status: PROPOSED      # ACCEPTED only by authorized maintainer
  approver_and_date: null
  passed_gate_id: null
  approved_slice_id: null       # required before engineering
  exact_allowed_paths: []
  explicitly_disallowed_changes: [production_code_without_permission, public_api_without_review]
  definition_of_done: [DOC-RESEARCH, DOC-CORE]
  test_commands_verified_on_revision: []
  rollback_and_stop_triggers: []
  next_owner_subsystem: LanguageSdk
```

## Acceptance policy

1. **Claude/research side** freezes sources, names competing hypotheses and attempts disconfirmation against independent controls. Result may recommend A/B over C. Missing experiment data must remain `NOT_YET_EVIDENCED` or `BLOCKED`.
2. **Research reviewer** verifies actual oracle independence, mutation detection and cost accounting. Reviewer cannot infer reproducibility from self-reported CI.
3. **Maintainer decision** separately records `ADR status=ACCEPTED`, `GXX PASS` and exactly one authorized implementation slice at a current pinned SHA. `PROPOSED` means Codex may only inspect and suggest tests, not mutate source.
4. **Codex implementer** checks SHA, allowed file list, DoD reference, validated baseline commands, forbidden dependency graph, negative oracle and safe rollback. If any missing, no code changes; return blockers.
5. **Integration reviewer** validates changed tree and affected consumers against baseline and attached receipts, not only textual summaries. G09/G10 require new approval; never broaden scope automatically.

## Independent critique checklist

**Research skeptic:** Is there a disconfirming holdout, A/B serious controls, all costs and independent oracle? **Compiler/API architect:** Did anyone duplicate global planning, introduce Wist into generic SDK, or require universal SSA? **Verification engineer:** Can a false-safe be detected by seeded mutants with exact replay? **External author:** Is handwritten typed C# demonstrably simpler and correct? Record each verdict and unresolved findings before proposing G09.
