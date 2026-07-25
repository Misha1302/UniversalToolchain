#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    (ROOT / path).write_text(text, encoding="utf-8", newline="\n")


def replace_exact(path: str, old: str, new: str, expected: int = 1) -> None:
    text = read(path)
    count = text.count(old)
    if count != expected:
        raise SystemExit(f"{path}: expected {expected} occurrence(s), found {count}: {old!r}")
    write(path, text.replace(old, new))


def replace_regex(path: str, pattern: str, replacement: str, expected: int = 1) -> None:
    text = read(path)
    updated, count = re.subn(pattern, replacement, text, flags=re.MULTILINE | re.DOTALL)
    if count != expected:
        raise SystemExit(f"{path}: expected {expected} regex occurrence(s), found {count}: {pattern!r}")
    write(path, updated)


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def replay_evidence(root: Path, fault_id: str, oracle_id: str, oracle_version: int, mechanism: str) -> dict:
    report_path = root / "replay-report.json"
    manifest_path = root / "MANIFEST.sha256"
    report = load_json(report_path)
    if not report.get("confirmedViolation") or report.get("flaky") or report.get("inconclusive") or report.get("infrastructureFailure"):
        raise SystemExit(f"{fault_id}: replay is not a clean confirmed violation")
    attempts = report.get("attempts", [])
    if len(attempts) != 3:
        raise SystemExit(f"{fault_id}: expected three attempts, got {len(attempts)}")
    fingerprints = {attempt["fingerprint"] for attempt in attempts}
    class_fingerprints = {attempt["classFingerprint"] for attempt in attempts}
    if len(fingerprints) != 1 or len(class_fingerprints) != 1:
        raise SystemExit(f"{fault_id}: replay fingerprints are unstable")
    return {
        "faultId": fault_id,
        "oracleId": oracle_id,
        "oracleVersion": oracle_version,
        "mechanism": mechanism,
        "repeat": 3,
        "confirmedViolation": True,
        "flaky": False,
        "inconclusive": False,
        "infrastructureFailure": False,
        "caseId": report["caseId"],
        "exactFingerprint": report["confirmedFingerprint"],
        "classFingerprint": report["confirmedClassFingerprint"],
        "replayReportSha256": sha256_file(report_path),
        "manifestSha256": sha256_file(manifest_path),
    }


def update_verification(total: str) -> None:
    replace_exact("Tools/check_documentation_status.py", "'1,465'", f"'{total}'")

    replace_exact("VERIFICATION.md", "| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.Tests` | 48 | 0 | 0 |")
    replace_exact("VERIFICATION.md", "| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.IntegrationTests` | 11 | 0 | 0 |")
    replace_exact("VERIFICATION.md", "| **Total** | **1,465** | **0** | **0** |", f"| **Total** | **{total}** | **0** | **0** |")
    replace_exact(
        "VERIFICATION.md",
        "- observation schema v4 with fail-closed surface/owner evidence contract v2 and typed trace completeness;",
        "- observation schema v5 with fail-closed surface/owner evidence contract v3, explicit single-extension surface/owner bindings and typed trace completeness;")
    replace_exact(
        "VERIFICATION.md",
        "- separate exact and class fingerprints;",
        "- separate exact and class fingerprints, with O-005 exact identity aggregating activation, route, owner-set and semantic violation dimensions;")
    replace_exact(
        "VERIFICATION.md",
        "- test-owned runtime-provider seeded faults for canonical SF-005 and SF-011, confirmed through ordinary observed activation evidence in three fresh processes.",
        "- test-owned runtime-provider seeded faults for canonical SF-005 and SF-011, confirmed through ordinary schema-v5 observed activation evidence in three fresh processes;\n- variant-scoped normalization of malformed Acme surface evidence without losing the plan snapshot or crashing the worker.")
    replace_exact(
        "VERIFICATION.md",
        "- observation and replay serialization compatibility;",
        "- observation and replay serialization compatibility across schema v1-v5, without upgrading schema-v4 evidence into current O-005 proof;")
    replace_exact(
        "VERIFICATION.md",
        "- seeded-fault detection without counting the seeded implementation as a product defect;",
        "- seeded-fault detection without counting the seeded implementation as a product defect;\n- strict O-005 rejection of unrelated exclusion-policy changes, unbound surface/owner deltas and incomplete extension declarations;\n- fresh-process SF-011 fingerprints that include both owner activation and semantic interference;")

    replace_exact("docs/evidence/current-verification.md", "1,465 tests succeeded", f"{total} tests succeeded")
    replace_exact("docs/evidence/current-verification.md", "| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.Tests` | 48 | 0 | 0 |")
    replace_exact("docs/evidence/current-verification.md", "| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.IntegrationTests` | 11 | 0 | 0 |")
    replace_exact("docs/evidence/current-verification.md", "| **Total** | **1,465** | **0** | **0** |", f"| **Total** | **{total}** | **0** | **0** |")
    replace_exact(
        "docs/evidence/current-verification.md",
        "Canonical SF-005 and SF-011 were each confirmed in three fresh processes through schema-v4 complete observed activation traces generated by test-owned runtime mechanisms.",
        "Canonical SF-005 and SF-011 were each confirmed in three fresh processes through schema-v5 complete observed activation traces generated by test-owned runtime mechanisms. SF-011 exact identity now includes both extension activation and the changed semantic result, while its coarser class identity omits concrete values.")


def update_status(sf005: dict, sf011: dict, total_plain: str) -> None:
    path = "internal-docs/proposals/planfuzz/implementation-status.md"
    replace_exact(
        path,
        "- Observation schema: version 4 with fail-closed surface/owner evidence contract v2; schema-v1 through schema-v3 observations remain readable, but legacy surface evidence cannot satisfy current O-004/O-005 proofs.",
        "- Observation schema: version 5 with fail-closed surface/owner evidence contract v3 and explicit independent-extension bindings; schema-v1 through schema-v4 observations remain readable, but schema-v4 binding-free evidence cannot satisfy current O-005 proof.")
    replace_exact(
        path,
        "- Surface evidence separates selected surface IDs from selected/excluded owner IDs, declares independent additions in both domains, records observed activated owners, uses a typed `Unsupported`/`Partial`/`Complete` trace status, and carries evidence-contract and route identities.",
        "- Surface evidence separates selected surface IDs from selected/excluded owner IDs, binds each declared independent extension to its exact surface and owner sets, records observed activated owners, uses a typed `Unsupported`/`Partial`/`Complete` trace status, and carries evidence-contract and route identities.")
    replace_exact(
        path,
        "- `O-005` derives baseline/extension direction structurally rather than from contract order, requires a pure additive delta in both surface and owner domains, and requires unchanged semantics, route identity and activation owners.",
        "- `O-005` derives baseline/extension direction structurally rather than from contract order, requires one exact new extension binding, preserves unrelated exclusion/declaration policy, and aggregates activation, route, activated-owner and semantic violations into deterministic exact/class identities.")
    replace_exact(
        path,
        "Observation schema v4 introduces surface evidence contract v2. Current evidence uses separate selected-surface, selected-owner, excluded-owner, independent-surface and independent-owner sets. Blank, duplicate, contradictory and out-of-domain IDs are rejected. Schema-v3 evidence remains readable for historical replay but is classified `Inconclusive` by current surface oracles.",
        "Observation schema v5 introduces surface evidence contract v3. Current evidence uses separate selected-surface, selected-owner and excluded-owner sets plus explicit independent-extension records binding each stable extension ID to its exact surface and owner sets. Blank, duplicate, contradictory, overlapping, unbound and out-of-domain IDs are rejected. Schema-v3 remains historical; schema-v4/evidence-v2 remains usable by O-004 but is `Inconclusive` for current O-005.")
    replace_exact(
        path,
        "- `O-005` extension noninterference derives the additive direction from evidence rather than contract order and requires equal semantics, selected route identity and activated-owner evidence.",
        "- `O-005` extension noninterference derives the additive direction from evidence rather than contract order, proves one strict extension delta including exclusion policy and binding identity, and aggregates every observed interference dimension before fingerprinting.")
    replace_regex(
        path,
        r"SF-005 excluded provider activation: confirmed 3/3, exact fingerprint [0-9a-f]{64}\nSF-011 extension noninterference:\s+confirmed 3/3, exact fingerprint [0-9a-f]{64}",
        f"SF-005 excluded provider activation: confirmed 3/3, exact fingerprint {sf005['exactFingerprint']}\nSF-011 extension noninterference:   confirmed 3/3, exact fingerprint {sf011['exactFingerprint']}")
    replace_exact(
        path,
        "The former Phase 3 records using `SF-002-excluded-owner-activation` and `SF-003-extension-noninterference` are superseded: those IDs conflicted with the canonical catalog and their fingerprints must not be mixed with the hardened evidence.",
        "The former Phase 3 records using `SF-002-excluded-owner-activation` and `SF-003-extension-noninterference` remain superseded. The schema-v4/O-005-v2 SF-011 fingerprint is also superseded because it stopped at owner activation and omitted the simultaneous semantic interference; it must not be mixed with schema-v5/O-005-v3 evidence.")
    replace_exact(path, "UniversalToolchain.PlanFuzz.Tests:             41 passed", "UniversalToolchain.PlanFuzz.Tests:             48 passed")
    replace_exact(path, "UniversalToolchain.PlanFuzz.IntegrationTests:  10 passed", "UniversalToolchain.PlanFuzz.IntegrationTests:  11 passed")
    replace_exact(path, "Total:                                      1465 passed", f"Total:                                      {total_plain} passed")

    replace_exact(
        "internal-docs/proposals/planfuzz/README.md",
        "+ fail-closed schema-v4 selected-surface/owner and observed activation evidence",
        "+ fail-closed schema-v5 surface/owner evidence with explicit independent-extension bindings")
    replace_exact(
        "docs/CURRENT_ARCHITECTURE_STATUS.md",
        "Observation schema v4 separates surface IDs from runtime owner IDs, rejects malformed evidence, and records actual Acme component activations.",
        "Observation schema v5 separates surface IDs from runtime owner IDs, binds one independent extension to its exact surfaces and owners, rejects malformed or policy-changing deltas, and records actual Acme component activations. O-005 exact fingerprints aggregate activation, route, owner-set and semantic interference instead of stopping at the first symptom.")


def update_specification() -> None:
    path = "internal-docs/proposals/planfuzz/technical-specification.ru.md"
    replacement = """## 16.2. Surface/owner evidence contract

Текущий observation schema — v5. Surface evidence contract v3 разделяет semantic surface IDs и runtime owner IDs и явно связывает каждый independent extension с обоими доменами:

```csharp
public sealed record IndependentExtensionEvidence(
    string ExtensionId,
    IReadOnlyList<string> SurfaceIds,
    IReadOnlyList<string> OwnerIds);

public sealed record SurfaceEvidence(
    int EvidenceContractVersion,
    IReadOnlyList<string> SelectedSurfaceIds,
    IReadOnlyList<string> SelectedOwnerIds,
    IReadOnlyList<string> ExcludedOwnerIds,
    IReadOnlyList<string> DeclaredIndependentSurfaceIds,
    IReadOnlyList<string> DeclaredIndependentOwnerIds,
    IReadOnlyList<IndependentExtensionEvidence> IndependentExtensions,
    IReadOnlyList<string> ActivatedOwnerIds,
    ActivationTraceStatus ActivationTraceStatus,
    string TraceKind,
    string RouteIdentity);
```

Инварианты fail-closed:

- blank, whitespace-surrounded и duplicate IDs rejected;
- selected и excluded owner sets disjoint;
- independent IDs являются subset соответствующего selected domain;
- activated owner должен быть selected либо explicitly excluded;
- `Complete` требует непустого selected-owner set;
- unknown evidence-contract version и unknown trace status rejected;
- extension IDs unique;
- каждый independent surface/owner принадлежит ровно одному binding;
- bindings покрывают declared-independent sets точно, без пропусков и лишних IDs;
- schema-v1..v3 остаются читаемыми для истории;
- schema-v4/evidence-v2 остаётся пригодной для O-004, но не может дать `Passed` текущему O-005 без explicit bindings.

`ActivationTraceStatus` — typed enum: `Unsupported`, `Partial`, `Complete`. Boolean completeness больше не является текущим контрактом.
"""
    replace_regex(
        path,
        r"## 16\.2\. Surface/owner evidence contract\n.*?`ActivationTraceStatus` — typed enum: `Unsupported`, `Partial`, `Complete`\. Boolean completeness больше не является текущим контрактом\.\n",
        replacement)

    o005 = """## O-005. Extension noninterference

### Проверяет

Добавление одного independent unused extension не меняет semantics, selected route или фактически activated owners для программы, которая extension не использует.

### Preconditions

- baseline/extension direction выводится из strict additive relation, а не из порядка variant IDs;
- в selected surface и selected owner domains нет удалений и есть непустые additions;
- additions точно совпадают с newly declared independent surface/owner IDs;
- появляется ровно один новый `IndependentExtensionEvidence`, который связывает exact added surfaces и owners одним stable extension ID;
- все прежние extension bindings сохраняются;
- `Extended.ExcludedOwnerIds = Baseline.ExcludedOwnerIds − AddedOwnerIds`; unrelated exclusion policy не меняется;
- current complete traces use the same evidence contract and `traceKind`;
- no override/shared slot conflict;
- no global side effect;
- source unchanged.

Malformed, non-additive, policy-changing, unbound или неоднозначная пара является contract/infrastructure failure, а не тихим `NotApplicable`.

Oracle не завершает evaluation на первом симптоме. Он детерминированно агрегирует:

```text
extension-owner activated
route identity changed
activated-owner set changed
observable semantics changed
```

Exact fingerprint содержит все конкретные observed dimensions; class fingerprint сохраняет категории без concrete values. Activation-only и activation-plus-semantic-interference обязаны иметь разные exact fingerprints, чтобы replay/reduction не подменял исходный механизм более слабым.

---
"""
    replace_regex(path, r"## O-005\. Extension noninterference\n.*?\n---\n", o005)

    replace_exact(
        path,
        "Seeded faults must execute inside test-owned package/runtime components and reach observations through normal instrumentation. Direct post-execution mutation of values, traces or owner sets does not satisfy mutation-adequacy evidence.",
        "Seeded faults must execute inside test-owned package/runtime components and reach observations through normal instrumentation. Direct post-execution mutation of values, traces or owner sets does not satisfy mutation-adequacy evidence. Multi-dimensional faults such as SF-011 must retain every observed violation dimension in the exact fingerprint; preserving only the first symptom is insufficient for replay/reduction identity.")


def update_evidence(clean_dir: Path, sf005_dir: Path, sf011_dir: Path) -> tuple[dict, dict]:
    evidence_path = ROOT / "internal-docs/proposals/planfuzz/evidence/phase3-surface-oracles-smoke-summary.json"
    previous = load_json(evidence_path)
    clean_summary_path = clean_dir / "summary.json"
    clean_manifest_path = clean_dir / "MANIFEST.sha256"
    clean = load_json(clean_summary_path)
    if clean.get("cleanCases") != 25 or clean.get("confirmedFindings") != 0 or clean.get("flakyCases") != 0 or clean.get("inconclusiveCases") != 0 or clean.get("infrastructureFailures") != 0:
        raise SystemExit("Clean 25-case campaign did not remain fully clean")

    sf005 = replay_evidence(
        sf005_dir,
        "SF-005-excluded-provider-activated",
        "O-004-negative-surface-preservation",
        2,
        "test-owned runtime provider invokes the excluded extension-owned activation hook")
    sf011 = replay_evidence(
        sf011_dir,
        "SF-011-extension-noninterference",
        "O-005-extension-noninterference",
        3,
        "test-owned runtime provider activates the bound extension owner and changes the result; O-005 exact identity aggregates both dimensions")
    if sf011["exactFingerprint"] == sf011["classFingerprint"]:
        raise SystemExit("SF-011 exact and class fingerprints must differ after multi-dimensional aggregation")

    updated = {
        "schemaVersion": 3,
        "date": "2026-07-25",
        "scope": "PlanFuzz Phase 3a O-005 evidence-identity hardening",
        "adapter": {
            "id": "acme-pricing",
            "version": "0.4.0",
            "generatorSchemaVersion": "acme-pricing-generator-v4",
        },
        "observationSchemaVersion": 5,
        "surfaceEvidenceContractVersion": 3,
        "traceKind": "observed-language-route-runtime-v3",
        "cleanCampaign": {
            "seed": clean["campaignSeed"],
            "requestedCases": clean["requestedCases"],
            "completedCases": clean["completedCases"],
            "repeat": 2,
            "cleanCases": clean["cleanCases"],
            "confirmedFindings": clean["confirmedFindings"],
            "distinctFindingClasses": clean["distinctFindingClasses"],
            "flakyCases": clean["flakyCases"],
            "inconclusiveCases": clean["inconclusiveCases"],
            "infrastructureFailures": clean["infrastructureFailures"],
            "summarySha256": sha256_file(clean_summary_path),
            "manifestSha256": sha256_file(clean_manifest_path),
        },
        "seededFaultReplays": [sf005, sf011],
        "supersedes": {
            "historicalMisnumberedEvidence": previous.get("supersedes"),
            "schemaV4O005V2Evidence": {
                "adapterVersion": "0.3.0",
                "observationSchemaVersion": 4,
                "surfaceEvidenceContractVersion": 2,
                "oracleVersion": 2,
                "sf011ExactFingerprint": "d4f4021a581a3dacd432aa85e97e602e586da371e9911b2b0757063e184923f3",
                "reason": "O-005 v2 stopped at extension-owner activation and omitted simultaneous semantic interference from exact identity.",
            },
        },
        "claimBoundary": "This is bounded Phase 3a stability and harness-identity evidence. The runtime faults are test-owned and do not count as discovered compiler defects. Lifecycle/session/concurrency schedules, schedule reduction, equal-budget superiority, publication novelty and production certification remain unverified.",
    }
    evidence_path.write_text(json.dumps(updated, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    return sf005, sf011


def regenerate_manifest() -> None:
    paths = subprocess.check_output(["git", "ls-files", "--cached", "-z"], cwd=ROOT).split(b"\0")
    with (ROOT / "MANIFEST.sha256").open("w", encoding="utf-8", newline="\n") as output:
        for raw_path in sorted(path for path in paths if path and path != b"MANIFEST.sha256"):
            path = raw_path.decode("utf-8")
            output.write(f"{sha256_file(ROOT / path)}  ./{path}\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--clean", type=Path, required=True)
    parser.add_argument("--sf005", type=Path, required=True)
    parser.add_argument("--sf011", type=Path, required=True)
    args = parser.parse_args()

    sf005, sf011 = update_evidence(args.clean, args.sf005, args.sf011)
    update_verification("1,473")
    update_status(sf005, sf011, "1473")
    update_specification()
    regenerate_manifest()

    stale = subprocess.run(
        [
            "git", "grep", "-n", "-E",
            "observation schema v4|Observation schema v4|schema-v4 complete observed|surface evidence contract v2|Total:[[:space:]]+1465 passed|\\*\\*1,465\\*\\*",
            "--",
            "VERIFICATION.md",
            "docs/CURRENT_ARCHITECTURE_STATUS.md",
            "docs/evidence/current-verification.md",
            "internal-docs/proposals/planfuzz",
        ],
        cwd=ROOT,
        text=True,
        capture_output=True,
    )
    if stale.returncode == 0:
        raise SystemExit("Stale Phase 3a evidence markers remain:\n" + stale.stdout)
    if stale.returncode not in (0, 1):
        raise SystemExit(stale.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
