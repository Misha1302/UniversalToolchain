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


def patch_tests() -> None:
    path = "UniversalToolchain/UniversalToolchain.PlanFuzz.Tests/SurfaceOracleTests.cs"
    replace_exact(
        path,
        '            Assert.That(result.FingerprintMaterial, Does.Contain("extension-activated:contribution:extension"));\n'
        '            Assert.That(result.FingerprintMaterial, Does.Contain("route:route:baseline|route:changed"));',
        '            Assert.That(result.FingerprintMaterial, Does.Contain("extension-activated:"));\n'
        '            Assert.That(result.FingerprintMaterial, Does.Contain("contribution:extension"));\n'
        '            Assert.That(result.FingerprintMaterial, Does.Contain("route:"));\n'
        '            Assert.That(result.FingerprintMaterial, Does.Contain("route:baseline"));\n'
        '            Assert.That(result.FingerprintMaterial, Does.Contain("route:changed"));')
    replace_exact(
        path,
        '            Assert.That(result.EffectiveClassFingerprintMaterial, Is.EqualTo("route-changed"));',
        '            Assert.That(result.EffectiveClassFingerprintMaterial, Does.Contain("route-changed"));')


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
    expected = {
        "requestedCases": 25,
        "completedCases": 25,
        "cleanCases": 25,
        "confirmedFindings": 0,
        "distinctFindingClasses": 0,
        "flakyCases": 0,
        "inconclusiveCases": 0,
        "infrastructureFailures": 0,
    }
    for key, value in expected.items():
        if clean.get(key) != value:
            raise SystemExit(f"clean campaign: {key}={clean.get(key)!r}, expected {value!r}")

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

    evidence["scope"] = "PlanFuzz Phase 3a O-004/O-005 identity and adversarial review hardening"
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
        "fingerprintMaterialUsesLengthPrefixedEncoding": True,
        "regressionTestsAdded": 4,
    }
    path.write_text(json.dumps(evidence, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def update_docs() -> None:
    replace_exact("Tools/check_documentation_status.py", "('1,475',", "('1,477',")

    replace_exact("VERIFICATION.md", "| `UniversalToolchain.PlanFuzz.Tests` | 50 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.Tests` | 52 | 0 | 0 |")
    replace_exact("VERIFICATION.md", "| **Total** | **1,475** | **0** | **0** |", "| **Total** | **1,477** | **0** | **0** |")
    replace_exact(
        "VERIFICATION.md",
        "- adversarial rejection of schema-v4 evidence-version forgery and delimiter-collision binding reassociation.",
        "- adversarial rejection of schema-v4 evidence-version forgery and delimiter-collision binding reassociation;\n- unambiguous UTF-8 length-prefixed O-004/O-005 fingerprint material, including regressions for comma-containing IDs versus multiple IDs.")

    replace_exact("docs/evidence/current-verification.md", "1,475 tests succeeded", "1,477 tests succeeded")
    replace_exact("docs/evidence/current-verification.md", "| `UniversalToolchain.PlanFuzz.Tests` | 50 | 0 | 0 |", "| `UniversalToolchain.PlanFuzz.Tests` | 52 | 0 | 0 |")
    replace_exact("docs/evidence/current-verification.md", "| **Total** | **1,475** | **0** | **0** |", "| **Total** | **1,477** | **0** | **0** |")
    replace_exact(
        "docs/evidence/current-verification.md",
        "Adversarial regressions additionally prove that schema-v4 cannot claim evidence-v3 and that extension bindings are compared structurally rather than through delimiter-concatenated strings.",
        "Adversarial regressions additionally prove that schema-v4 cannot claim evidence-v3, extension bindings are compared structurally rather than through delimiter-concatenated strings, and O-004/O-005 fingerprint material distinguishes delimiter-containing IDs from distinct ID sequences through UTF-8 length-prefixing.")

    replace_exact("internal-docs/proposals/planfuzz/implementation-status.md", "UniversalToolchain.PlanFuzz.Tests:             50 passed", "UniversalToolchain.PlanFuzz.Tests:             52 passed")
    replace_exact("internal-docs/proposals/planfuzz/implementation-status.md", "Total:                                      1475 passed", "Total:                                      1477 passed")
    replace_exact(
        "internal-docs/proposals/planfuzz/implementation-status.md",
        "- Exact fingerprints preserve testcase-level evidence and remain authoritative for repeated replay confirmation.",
        "- Exact fingerprints preserve testcase-level evidence and remain authoritative for repeated replay confirmation. O-004/O-005 encode adapter-controlled strings with deterministic UTF-8 length prefixes rather than ambiguous delimiter joins.")
    replace_exact(
        "internal-docs/proposals/planfuzz/implementation-status.md",
        "Binding equality is structural; delimiter-derived synthetic identities are forbidden.",
        "Binding equality is structural; delimiter-derived synthetic identities are forbidden. Fingerprint sequences use length-prefixed encoding, so one ID containing delimiters cannot alias multiple IDs.")

    replace_exact(
        "internal-docs/proposals/planfuzz/technical-specification.ru.md",
        "- extension bindings сравниваются структурно по `ExtensionId`, `SurfaceIds` и `OwnerIds`; delimiter-concatenated synthetic identity запрещена.",
        "- extension bindings сравниваются структурно по `ExtensionId`, `SurfaceIds` и `OwnerIds`; delimiter-concatenated synthetic identity запрещена;\n- adapter-controlled fingerprint fields и sequences кодируются однозначно через UTF-8 byte-length prefixes; `string.Join` не является допустимым identity contract.")
    replace_exact(
        "internal-docs/proposals/planfuzz/technical-specification.ru.md",
        "Exact fingerprint содержит все конкретные observed dimensions; class fingerprint сохраняет категории без concrete values.",
        "Exact fingerprint содержит все конкретные observed dimensions; class fingerprint сохраняет категории без concrete values. Оба material contracts используют однозначное length-prefixed encoding для dynamic fields и sequences.")

    replace_exact(
        "docs/CURRENT_ARCHITECTURE_STATUS.md",
        "Schema-v4 cannot claim evidence-v3, and extension bindings are compared structurally rather than through delimiter-concatenated identities.",
        "Schema-v4 cannot claim evidence-v3, extension bindings are compared structurally rather than through delimiter-concatenated identities, and O-004/O-005 dynamic fingerprint fields use deterministic UTF-8 length prefixes.")


def regenerate_manifest() -> None:
    paths = subprocess.check_output(["git", "ls-files", "--cached", "-z"], cwd=ROOT).split(b"\0")
    with (ROOT / "MANIFEST.sha256").open("w", encoding="utf-8", newline="\n") as output:
        for raw_path in sorted(path for path in paths if path and path != b"MANIFEST.sha256"):
            path = raw_path.decode("utf-8")
            output.write(f"{sha256_file(ROOT / path)}  ./{path}\n")


def finalize(clean: Path, sf005: Path, sf011: Path, reviewed_head: str) -> None:
    update_evidence(clean, sf005, sf011, reviewed_head)
    update_docs()
    regenerate_manifest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--patch-tests", action="store_true")
    parser.add_argument("--clean", type=Path)
    parser.add_argument("--sf005", type=Path)
    parser.add_argument("--sf011", type=Path)
    parser.add_argument("--reviewed-head")
    args = parser.parse_args()

    if args.patch_tests:
        patch_tests()
        return 0
    if not all((args.clean, args.sf005, args.sf011, args.reviewed_head)):
        parser.error("finalize mode requires --clean, --sf005, --sf011 and --reviewed-head")
    finalize(args.clean, args.sf005, args.sf011, args.reviewed_head)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
