# One canonical research base — ownership, IDs, traceability and update workflow

**Policy proposal and editorial map (2026-10-12).** This directory, `internal-docs/research/ideal-ut-2026-10-08/`, is the **existing canonical entrypoint** for Ideal UT research. **Do not create `ideal-ut-v2`, `research-final`, a second roadmap, or a new authoritative 222-item register.** The date in its existing path is lineage, not a claim of frozen contents; renaming it would break links and add needless migration risk.

## Where truth lives

| Question | Canonical owner | Other documents' role |
| --- | --- | --- |
| Current merged implementation / public API | [Current architecture](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) + actual code/test evidence at specific HEAD | link, never certify behavior by copying a research claim |
| Target proposed architecture | [ARCHITECTURE.md](ARCHITECTURE.md), with explicitly dated [independent review](REVIEW_2026-10-12.md) | critique and proposals, not current implementation |
| Candidate hypotheses and choice status | [IDEAS.md](IDEAS.md) | all new review documents link, do not maintain independent `ACCEPTED` flags |
| Existing project gap register | [CURRENT_VS_IDEAL.md](CURRENT_VS_IDEAL.md) | revision-bound IUT-01–13 and evidence limit |
| Falsification program | [EXPERIMENTS.md](EXPERIMENTS.md), [addendum](FALSIFICATION_ADDENDUM_2026-10-12.md) | E1–E8 IDs remain stable |
| Experimental development slices | [ENGINEERING_BACKLOG.md](ENGINEERING_BACKLOG.md) | S0–S8, not a second approved ticket list |
| Conditional roadmap | [ROADMAP.md](ROADMAP.md) | M0–M6 status changes require commit/test evidence |
| 222 **candidate** items | [DEVELOPMENT_INVENTORY_2026-10-09.md](DEVELOPMENT_INVENTORY_2026-10-09.md) | preserve exact 15-section structure, cross-reference IDs; never auto-promote into mandated roadmap |
| Provenance of earlier research packages | [RESEARCH_TRACEABILITY.md](RESEARCH_TRACEABILITY.md) and [SOURCES.md](SOURCES.md) | exact archives absent from repo remain external historical dependencies |
| Source handwritten IUT note | user-supplied transcript `IUT_заметки_расшифровка.md` (not checked into this branch) | [annotated review](ANNOTATED_NOTE_2026-10-12.md) quotes abbreviated text with ambiguities intact |
| Scientific competitors | [STRATEGY_PRIOR_ART.md](STRATEGY_PRIOR_ART.md), [new dated comparison](PRIOR_ART_DELTA_2026-10-12.md) | record source delta without claiming novelty |
| Whole-language source-to-result research | [CGO27 results](../../../CGO27/RESULTS_SUMMARY.md) and [claims](../../../CGO27/CLAIM_EVIDENCE_LEDGER.md) | bounded evidence only, not new Ideal UT implementation |

Do not use Markdown to create a second runtime metadata database. One idea has one current owner; an index or review may contain a **dated observation** but must link to that owner rather than maintain duplicate status.

## Stable traceability and evidence discipline

Prefer current IDs `IUT-01..13`, `E1..8`, `M0..6`, `S0..8`. Before defining `IDEA-nnn`, check the existing `IDEAS.md`, IUT gap register, named inventory entry and open PR/issue. If a truly independent hypothesis is introduced, assign a stable ID only in the owner, link to it elsewhere; no retroactive renumbering.

The desired chain is:

```text
historical note item / source offset
  -> research question (IUT-n or stable idea ID)
  -> external literature or repository evidence (URL, SHA, date)
  -> alternatives (incl. ordinary typed interfaces)
  -> proposal / accepted decision (one owner)
  -> falsification experiment (E / M / S)
  -> raw test result + independent oracle + exact build SHA
  -> change in IDEAS status / specific ROADMAP gate
  -> merged implementation + CI/release -> current architecture
```

A working spike, diagram, PR description, source generator, passing toy test, authored contract or route hash is insufficient for “implemented.” A new `master` HEAD invalidates any silent inference that an old snapshot describes unchanged behavior.

## Status vocabulary and promotion evidence

- `PROPOSED`: articulation only.
- `INVESTIGATING`: question, adversarial alternatives and test planned; **not** proof.
- `EXPERIMENTAL`: bounded implementation with code and reproducible results; specify *scope and revision*.
- `ACCEPTED`: explicit ADR/research decision in its canonical owner, not implied by reviewer preference.
- `IMPLEMENTED`: code merged at named revision, relevant tests and public contract checked for claimed scope.
- `REJECTED`: recorded failure or simpler winning baseline, including rationale and exact experiment.
- `SUPERSEDED`: successor ID and migration link.

Never store these statuses independently in every roadmap and overview. `IDEAS.md` owns candidate decisions; `CURRENT_ARCHITECTURE_STATUS.md` owns current merged implementation. If a historical item has different status in an old report, leave the old evidence intact and add a supersession link.

## The 222-item register is a catalogue, not execution authority

At `master@886bf241` the current inventory contains **222 lines matching its bracket-tagged candidate pattern across 15 substantive categories** (counted from its actual GitHub file). That is a content-count verification, *not* a correctness or priority validation.

Crosswalk first, never rewrite all 222 rows just to match a new taxonomy. Identify duplicates by same semantic owner and decision (e.g. versioned ProgramSnapshot vs evidence scope; `FactStore` and `query` vs indexed cache; “MCP surface” vs AI tool adapters); link synonyms to one canonical record. Keep the source row as historical inventory until targeted evidence justifies a merge or rejection; don't delete potentially useful negatives.

For the first decision-driving slice, prioritize: program/revision/implementation identity, independent oracle, selected-world truth boundary, original consumer + novel provider N+1, adversarial negative evidence, cross-IR witness/reanalysis, and whole-cost baseline. Defer persistent planner caches, global Horn inference, full IDE/workbench, universal algebra and proof-carrying optimizations until proven demands. This **is an independent prioritization proposal**, not a reclassification of all 222 candidate statuses.

## Low-overhead update procedure for human and AI contributors

1. **Observe:** capture current default branch, exact HEAD, current public owner files and changes from previous review; inspect any open PR but treat unmerged code as a separate branch.
2. **Search before writing:** check current IDs/titles in `IDEAS.md`, `DEVELOPMENT_INVENTORY`, and relevant ADR, issues/PRs; choose a single canonical owner.
3. **State the problem:** one question, counterexample, alternative, affected layers and explicit evidence level. Preserve ambiguity in the source note.
4. **Compare strongest baseline:** direct C# or mature framework; state situations where *not implementing* wins.
5. **Draft experiment:** add to the corresponding E/M/S owner, pin oracle and falsification; no new numbering if existing ID suffices.
6. **Run and record:** separate the source/test SHA from project/research document SHA; save raw measurements, negative results, commands, environment, independent holdouts and scope.
7. **Decision:** update `IDEAS.md` once, cite alternatives, revision and evidence; attach PR/ADR link as appropriate.
8. **Documentation:** update `ROADMAP.md` only on gated status change, then `CURRENT_ARCHITECTURE_STATUS.md` only after code merge/test proof.
9. **Historical drift:** mark outdated review as `SUPERSEDED BY...` via dated header/link, not destructive rewriting of historical findings.
10. **Navigation:** keep this folder's `README.md` as single entrypoint, run relative-link checks, ID/duplicate-owner grep and scope audit.

## Minimum mechanical validation for docs-only PRs

- Verify changed paths are restricted to this directory unless a root pointer must change.
- Verify new relative Markdown links resolve against the fetched master tree and newly created files; external links remain grounded in inspected official/paper sources.
- Verify 15 IR note questions are individually numbered once in [review](ANNOTATED_NOTE_2026-10-12.md).
- Verify inventory line count remains unchanged and older research files remain byte-identical except intentional index update.
- Verify no sentence says a PR-only behavior is merged or E1/E2/M1 is completed.
- Verify follow-up owner `README.md` indexes all new documents; a local docs-only read does not constitute compiler tests.

## Deliberate omissions

No new `VISION.md` /`IDEAS-v2.md` /`ROADMAP-2026-10-12.md` hierarchy, no dynamic Datalog knowledge database, no deletion of historical notes and no bulk migration of the 222 candidate rows. These would create the drift problem the research base is supposed to solve.
