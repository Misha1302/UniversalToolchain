#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path.cwd()


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
    exact = {attempt["fingerprint"] for attempt in attempts}
    classes = {attempt["classFingerprint"] for attempt in attempts}
    if len(exact) != 1 or len(classes) != 1:
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


def update_evidence(clean_dir: Path, sf005_dir: Path, sf011_dir: Path, reviewed_head: str) -> None:
    path = ROOT / "internal-docs/proposals/planfuzz/evidence/phase3-surface-oracles-smoke-summary.json"
    evidence = load_json(path)
    clean_summary_path = clean_dir / "summary.json"
    clean_manifest_path = clean_dir / "MANIFEST.sha256"
    clean = load_json(clean_summary_path)
    required_clean = {
        "requestedCases": 25,
        "completedCases": 25,
        "cleanCases": 25,
        "confirmedFindings": 0,
        "distinctFindingClasses": 0,
        "flakyCases": 0,
        "inconclusiveCases": 0,
        "infrastructureFailures": 0,
    }
    for key, expected in required_clean.items():
        if clean.get(key) != expected:
            raise SystemExit(f"clean campaign: {key}={clean.get(key)!r}, expected {expected!r}")

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
        raise SystemExit("SF-011 exact and class fingerprints must differ")

    evidence["scope"] = "PlanFuzz Phase 3a O-005 evidence-identity and adversarial review hardening"
    evidence["reviewedImplementationHead"] = reviewed_head
    evidence["cleanCampaign"] = {
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
    }
    evidence["seededFaultReplays"] = [sf005, sf011]
    evidence["adversarialReview"] = {
        "schemaV4CannotClaimEvidenceContractV3": True,
        "extensionBindingsComparedStructurally": True,
        "delimiterDerivedBindingIdentityForbidden": True,
        "regressionTestsAdded": 2,
    }
    path.write_text(json.dumps(evidence, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def update_docs() -> None:
    replace_exact("Tools/check_documentation_status.py", "('1,473',", "('1,475',")

    replace_exact("VERIFICATION.md", "| `UniversalToolchain.PlanFuzz.Tests` | 48 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.Tests` | 50 | 0 | 0 |")
    replace_exact("VERIFICATION.md", "| **Total** | **1,473** | **0** | **0** |", "| **Total** | **1,475** | **0** | **0** |")
    replace_exact(
        "VERIFICATION.md",
        "- observation and replay serialization compatibility across schema v1-v5, without upgrading schema-v4 evidence into current O-005 proof;",
        "- observation and replay serialization compatibility across schema v1-v5; schema-v4 is fixed to evidence contract v2 and cannot forge current v3 proof;")
    replace_exact(
        "VERIFICATION.md",
        "- strict O-005 rejection of unrelated exclusion-policy changes, unbound surface/owner deltas and incomplete extension declarations;",
        "- strict O-005 rejection of unrelated exclusion-policy changes, unbound surface/owner deltas, incomplete declarations and structurally reassociated extension bindings;")
    replace_exact(
        "VERIFICATION.md",
        "- variant-scoped normalization of malformed Acme surface evidence without losing the plan snapshot or crashing the worker.",
        "- variant-scoped normalization of malformed Acme surface evidence without losing the plan snapshot or crashing the worker;\n- adversarial rejection of schema-v4 evidence-version forgery and delimiter-collision binding reassociation.")

    replace_exact("docs/evidence/current-verification.md", "1,473 tests succeeded", "1,475 tests succeeded")
    replace_exact("docs/evidence/current-verification.md", "| `UniversalToolchain.PlanFuzz.Tests` | 48 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.Tests` | 50 | 0 | 0 |")
    replace_exact("docs/evidence/current-verification.md", "| **Total** | **1,473** | **0** | **0** |", "| **Total** | **1,475** | **0** | **0** |")
    replace_exact(
        "docs/evidence/current-verification.md",
        "SF-011 exact identity now includes both extension activation and the changed semantic result, while its coarser class identity omits concrete values.",
        "SF-011 exact identity now includes both extension activation and the changed semantic result, while its coarser class identity omits concrete values. Adversarial regressions additionally prove that schema-v4 cannot claim evidence-v3 and that extension bindings are compared structurally rather than through delimiter-concatenated strings.")

    replace_exact("internal-docs/proposals/planfuzz/implementation-status.md", "UniversalToolchain.PlanFuzz.Tests:             48 passed", "UniversalToolchain.PlanFuzz.Tests:             50 passed")
    replace_exact("internal-docs/proposals/planfuzz/implementation-status.md", "Total:                                      1473 passed", "Total:                                      1475 passed")
    replace_exact(
        "internal-docs/proposals/planfuzz/implementation-status.md",
        "- Observation schema: version 5 with fail-closed surface/owner evidence contract v3 and explicit independent-extension bindings; schema-v1 through schema-v4 observations remain readable, but schema-v4 binding-free evidence cannot satisfy current O-005 proof.",
        "- Observation schema: version 5 with fail-closed surface/owner evidence contract v3 and explicit independent-extension bindings; schema-v1 through schema-v4 observations remain readable, schema-v4 is fixed to evidence contract v2, and binding-free evidence cannot satisfy current O-005 proof.")
    replace_exact(
        "internal-docs/proposals/planfuzz/implementation-status.md",
        "- `O-005` derives baseline/extension direction structurally rather than from contract order, requires one exact new extension binding, preserves unrelated exclusion/declaration policy, and aggregates activation, route, activated-owner and semantic violations into deterministic exact/class identities.",
        "- `O-005` derives baseline/extension direction structurally rather than from contract order, compares extension IDs and exact surface/owner sets structurally, requires one exact new binding, preserves unrelated exclusion/declaration policy, and aggregates activation, route, activated-owner and semantic violations into deterministic exact/class identities.")
    replace_exact(
        "internal-docs/proposals/planfuzz/implementation-status.md",
        "Observation schema v5 introduces surface evidence contract v3. Current evidence uses separate selected-surface, selected-owner and excluded-owner sets plus explicit independent-extension records binding each stable extension ID to its exact surface and owner sets. Blank, duplicate, contradictory, overlapping, unbound and out-of-domain IDs are rejected. Schema-v3 remains historical; schema-v4/evidence-v2 remains usable by O-004 but is `Inconclusive` for current O-005.",
        "Observation schema v5 introduces surface evidence contract v3. Current evidence uses separate selected-surface, selected-owner and excluded-owner sets plus explicit independent-extension records binding each stable extension ID to its exact surface and owner sets. Blank, duplicate, contradictory, overlapping, unbound and out-of-domain IDs are rejected. Schema-v3 remains historical; schema-v4 is constrained to evidence-v2, remains usable by O-004, and is `Inconclusive` for current O-005. Binding equality is structural; delimiter-derived synthetic identities are forbidden.")

    replace_exact(
        "internal-docs/proposals/planfuzz/technical-specification.ru.md",
        "- schema-v4/evidence-v2 остаётся пригодной для O-004, но не может дать `Passed` текущему O-005 без explicit bindings.",
        "- observation schema владеет допустимой evidence version: schema-v4 принимает только evidence-v2 и не может объявить себя v3;\n- schema-v4/evidence-v2 остаётся пригодной для O-004, но не может дать `Passed` текущему O-005 без explicit bindings;\n- extension bindings сравниваются структурно по `ExtensionId`, `SurfaceIds` и `OwnerIds`; delimiter-concatenated synthetic identity запрещена.")
    replace_exact(
        "internal-docs/proposals/planfuzz/technical-specification.ru.md",
        "- все прежние extension bindings сохраняются;",
        "- все прежние extension bindings сохраняются и сравниваются структурно, без строковой delimiter-кодировки;")

    replace_exact(
        "docs/CURRENT_ARCHITECTURE_STATUS.md",
        "O-005 exact fingerprints aggregate activation, route, owner-set and semantic interference instead of stopping at the first symptom.",
        "O-005 exact fingerprints aggregate activation, route, owner-set and semantic interference instead of stopping at the first symptom. Schema-v4 cannot claim evidence-v3, and extension bindings are compared structurally rather than through delimiter-concatenated identities.")


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
    parser.add_argument("--reviewed-head", required=True)
    args = parser.parse_args()

    update_evidence(args.clean, args.sf005, args.sf011, args.reviewed_head)
    update_docs()
    regenerate_manifest()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
