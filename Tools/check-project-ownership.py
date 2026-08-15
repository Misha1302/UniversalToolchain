#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import json
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path


class OwnershipError(ValueError):
    pass


@dataclass(frozen=True)
class OwnershipManifest:
    wist: frozenset[str]
    universal_globs: tuple[str, ...]
    forbidden_universal_tokens: tuple[str, ...]
    legacy_artifact_fields: tuple[str, ...]


def posix(path: Path) -> str:
    return path.as_posix()


def load_manifest(root: Path) -> OwnershipManifest:
    path = root / "eng" / "project-ownership.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != 1:
        raise OwnershipError("eng/project-ownership.json must use schemaVersion 1")
    owners = data.get("owners")
    if not isinstance(owners, dict):
        raise OwnershipError("owners must be an object")
    wist = owners.get("WIST_PRODUCT")
    universal = owners.get("UNIVERSAL")
    if not isinstance(wist, list) or not all(isinstance(v, str) for v in wist):
        raise OwnershipError("owners.WIST_PRODUCT must be a string array")
    if len(wist) != len(set(wist)):
        raise OwnershipError("owners.WIST_PRODUCT contains duplicate paths")
    if not isinstance(universal, list) or not all(isinstance(v, str) for v in universal):
        raise OwnershipError("owners.UNIVERSAL must be a string array")
    classification = data.get("classification", {})
    if classification.get("precedence") != ["WIST_PRODUCT", "UNIVERSAL"]:
        raise OwnershipError("classification precedence must be WIST_PRODUCT then UNIVERSAL")
    if classification.get("requireTotalCoverage") is not True:
        raise OwnershipError("classification must require total coverage")
    forbidden = data.get("forbiddenUniversalSourceTokens", [])
    legacy = data.get("legacyArtifactFieldAllowlist", [])
    if not all(isinstance(v, str) and v for v in forbidden):
        raise OwnershipError("forbiddenUniversalSourceTokens must contain non-empty strings")
    if not all(isinstance(v, str) and v for v in legacy):
        raise OwnershipError("legacyArtifactFieldAllowlist must contain non-empty strings")
    return OwnershipManifest(
        wist=frozenset(wist),
        universal_globs=tuple(universal),
        forbidden_universal_tokens=tuple(forbidden),
        legacy_artifact_fields=tuple(legacy),
    )


def all_projects(root: Path) -> list[Path]:
    ignored = {"bin", "obj", "artifacts", ".git", "node_modules"}
    result: list[Path] = []
    for path in root.rglob("*.csproj"):
        if any(part in ignored for part in path.parts):
            continue
        result.append(path)
    return sorted(result)


def classify(relative: str, manifest: OwnershipManifest) -> str:
    if relative in manifest.wist:
        return "WIST_PRODUCT"
    if any(fnmatch.fnmatchcase(relative, pattern) for pattern in manifest.universal_globs):
        return "UNIVERSAL"
    raise OwnershipError(f"project has no owner: {relative}")


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def project_references(project: Path) -> list[Path]:
    try:
        root = ET.parse(project).getroot()
    except ET.ParseError as exc:
        raise OwnershipError(f"invalid project XML {project}: {exc}") from exc
    refs: list[Path] = []
    for element in root.iter():
        if local_name(element.tag) != "ProjectReference":
            continue
        include = element.attrib.get("Include", "").strip()
        if not include or "$" in include:
            continue
        refs.append((project.parent / include.replace("\\", "/")).resolve())
    return refs


def require_graph_direction(root: Path, manifest: OwnershipManifest) -> dict[Path, str]:
    projects = all_projects(root)
    owners: dict[Path, str] = {}
    for project in projects:
        relative = posix(project.relative_to(root))
        owners[project.resolve()] = classify(relative, manifest)

    missing_declared = sorted(
        relative for relative in manifest.wist if not (root / relative).is_file()
    )
    if missing_declared:
        raise OwnershipError(
            "WIST_PRODUCT manifest paths do not exist: " + ", ".join(missing_declared)
        )

    violations: list[str] = []
    for project in projects:
        source = project.resolve()
        if owners[source] != "UNIVERSAL":
            continue
        for target in project_references(project):
            target_owner = owners.get(target)
            if target_owner == "WIST_PRODUCT":
                violations.append(
                    f"{posix(project.relative_to(root))} -> {posix(target.relative_to(root))}"
                )
    if violations:
        raise OwnershipError(
            "UNIVERSAL -> WIST_PRODUCT ProjectReference is forbidden: "
            + "; ".join(sorted(violations))
        )
    return owners


def require_universal_sources_language_neutral(
    root: Path, manifest: OwnershipManifest, owners: dict[Path, str]
) -> None:
    violations: list[str] = []
    for project, owner in owners.items():
        if owner != "UNIVERSAL":
            continue
        project_dir = project.parent
        for source in project_dir.rglob("*.cs"):
            if any(part in {"bin", "obj"} for part in source.parts):
                continue
            text = source.read_text(encoding="utf-8-sig", errors="strict")
            hit = next((token for token in manifest.forbidden_universal_tokens if token in text), None)
            if hit is not None:
                violations.append(f"{posix(source.relative_to(root))}: {hit}")
    if violations:
        raise OwnershipError(
            "UNIVERSAL production/test source contains Wist-owned surface: "
            + "; ".join(sorted(violations))
        )


def require_exact_legacy_artifact_allowlist(manifest: OwnershipManifest) -> None:
    expected = {
        "WistSyntaxArtifact.Modules",
        "WistBytecodeArtifact.FrontendModules",
        "WistAirArtifact.FrontendModules",
        "WistAirArtifact.Optimizers",
    }
    actual = set(manifest.legacy_artifact_fields)
    if actual != expected:
        raise OwnershipError(
            "legacy artifact allowlist must name exactly the audited executable fields; "
            f"expected={sorted(expected)}, actual={sorted(actual)}"
        )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate deterministic UT/Wist project ownership and dependency direction."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        manifest = load_manifest(root)
        require_exact_legacy_artifact_allowlist(manifest)
        owners = require_graph_direction(root, manifest)
        require_universal_sources_language_neutral(root, manifest, owners)
    except (OSError, ValueError, OwnershipError) as exc:
        print(f"PROJECT_OWNERSHIP=FAIL: {exc}", file=sys.stderr)
        return 1
    counts = {owner: list(owners.values()).count(owner) for owner in ("UNIVERSAL", "WIST_PRODUCT")}
    print(
        "PROJECT_OWNERSHIP=PASS "
        f"UNIVERSAL={counts['UNIVERSAL']} WIST_PRODUCT={counts['WIST_PRODUCT']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
