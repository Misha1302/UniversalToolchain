#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import json
import re
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
    data = json.loads((root / "eng" / "project-ownership.json").read_text(encoding="utf-8"))
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
    if classification.get("precedence") != ["WIST_PRODUCT", "UNIVERSAL"] or classification.get("requireTotalCoverage") is not True:
        raise OwnershipError("classification must be total with WIST_PRODUCT precedence")
    forbidden = data.get("forbiddenUniversalSourceTokens", [])
    legacy = data.get("legacyArtifactFieldAllowlist", [])
    if not all(isinstance(v, str) and v for v in forbidden):
        raise OwnershipError("forbiddenUniversalSourceTokens must contain non-empty strings")
    if not isinstance(legacy, list) or not all(isinstance(v, str) and v for v in legacy):
        raise OwnershipError("legacyArtifactFieldAllowlist must be a string array")
    return OwnershipManifest(frozenset(wist), tuple(universal), tuple(forbidden), tuple(legacy))


def all_projects(root: Path) -> list[Path]:
    ignored = {"bin", "obj", "artifacts", ".git", "node_modules"}
    return sorted(path for path in root.rglob("*.csproj") if not any(part in ignored for part in path.parts))


def classify(relative: str, manifest: OwnershipManifest) -> str:
    if relative in manifest.wist:
        return "WIST_PRODUCT"
    if any(fnmatch.fnmatchcase(relative, pattern) for pattern in manifest.universal_globs):
        return "UNIVERSAL"
    raise OwnershipError(f"project has no owner: {relative}")


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def parse_project(project: Path) -> ET.Element:
    try:
        return ET.parse(project).getroot()
    except ET.ParseError as exc:
        raise OwnershipError(f"invalid project XML {project}: {exc}") from exc


def project_references(project: Path) -> list[Path]:
    root = parse_project(project)
    refs: list[Path] = []
    for element in root.iter():
        if local_name(element.tag) != "ProjectReference":
            continue
        include = element.attrib.get("Include", "").strip()
        if include and "$" not in include:
            refs.append((project.parent / include.replace("\\", "/")).resolve())
    return refs


def first_property(project_root: ET.Element, property_name: str) -> str | None:
    for element in project_root.iter():
        if local_name(element.tag) != property_name:
            continue
        value = (element.text or "").strip()
        if value and "$" not in value:
            return value
    return None


def project_identities(project: Path) -> frozenset[str]:
    project_root = parse_project(project)
    identities = {project.stem}
    for property_name in ("AssemblyName", "PackageId"):
        value = first_property(project_root, property_name)
        if value:
            identities.add(value)
    return frozenset(identities)


def require_graph_direction(root: Path, manifest: OwnershipManifest) -> dict[Path, str]:
    projects = all_projects(root)
    owners = {project.resolve(): classify(posix(project.relative_to(root)), manifest) for project in projects}
    missing = sorted(relative for relative in manifest.wist if not (root / relative).is_file())
    if missing:
        raise OwnershipError("WIST_PRODUCT manifest paths do not exist: " + ", ".join(missing))
    violations: list[str] = []
    for project in projects:
        source = project.resolve()
        if owners[source] != "UNIVERSAL":
            continue
        for target in project_references(project):
            if owners.get(target) == "WIST_PRODUCT":
                violations.append(f"{posix(project.relative_to(root))} -> {posix(target.relative_to(root))}")
    if violations:
        raise OwnershipError("UNIVERSAL -> WIST_PRODUCT ProjectReference is forbidden: " + "; ".join(sorted(violations)))
    return owners


def wist_identity_map(root: Path, manifest: OwnershipManifest) -> dict[str, str]:
    identities: dict[str, str] = {}
    for relative in sorted(manifest.wist):
        project = root / relative
        if not project.is_file():
            continue
        for identity in project_identities(project):
            key = identity.casefold()
            previous = identities.get(key)
            if previous is not None and previous != relative:
                raise OwnershipError(
                    f"WIST_PRODUCT projects expose ambiguous assembly/package identity '{identity}': {previous}; {relative}")
            identities[key] = relative
    return identities


def normalized_identity(value: str) -> str:
    return value.split(",", 1)[0].strip().casefold()


_IVT_SOURCE = re.compile(r"InternalsVisibleTo\s*\(\s*\"([^\"]+)\"")


def require_no_hidden_reverse_identity_edges(
    root: Path,
    manifest: OwnershipManifest,
    owners: dict[Path, str],
) -> None:
    wist_identities = wist_identity_map(root, manifest)
    wist_project_paths = {
        posix((root / relative).resolve()).casefold(): relative
        for relative in manifest.wist
    }
    violations: list[str] = []

    for project, owner in owners.items():
        if owner != "UNIVERSAL":
            continue
        relative_project = posix(project.relative_to(root))
        project_root = parse_project(project)

        for element in project_root.iter():
            kind = local_name(element.tag)
            if kind not in {"PackageReference", "Reference", "InternalsVisibleTo", "Analyzer", "Import"}:
                continue
            raw = (element.attrib.get("Include") or element.attrib.get("Project") or element.text or "").strip()
            if not raw:
                continue

            identity = normalized_identity(raw)
            target = wist_identities.get(identity)
            if target is not None:
                violations.append(f"{relative_project}: {kind} '{raw}' -> {target}")
                continue

            if "$" not in raw and kind in {"Analyzer", "Import"}:
                candidate = (project.parent / raw.replace("\\", "/")).resolve()
                candidate_text = posix(candidate).casefold()
                direct = wist_project_paths.get(candidate_text)
                if direct is not None:
                    violations.append(f"{relative_project}: {kind} '{raw}' -> {direct}")
                    continue
                for wist_project_path, wist_relative in wist_project_paths.items():
                    wist_directory = str(Path(wist_project_path).parent).replace("\\", "/").rstrip("/") + "/"
                    if candidate_text.startswith(wist_directory):
                        violations.append(f"{relative_project}: {kind} '{raw}' enters {wist_relative}")
                        break

        for source in project.parent.rglob("*.cs"):
            if any(part in {"bin", "obj"} for part in source.parts):
                continue
            text = source.read_text(encoding="utf-8-sig", errors="strict")
            for match in _IVT_SOURCE.finditer(text):
                friend = match.group(1)
                target = wist_identities.get(normalized_identity(friend))
                if target is not None:
                    violations.append(
                        f"{posix(source.relative_to(root))}: InternalsVisibleTo('{friend}') -> {target}")

    if violations:
        raise OwnershipError(
            "UNIVERSAL -> WIST_PRODUCT package/assembly/friend/build edge is forbidden: " +
            "; ".join(sorted(set(violations))))


def require_universal_sources_language_neutral(root: Path, manifest: OwnershipManifest, owners: dict[Path, str]) -> None:
    violations: list[str] = []
    for project, owner in owners.items():
        if owner != "UNIVERSAL":
            continue
        for source in project.parent.rglob("*.cs"):
            if any(part in {"bin", "obj"} for part in source.parts):
                continue
            text = source.read_text(encoding="utf-8-sig", errors="strict")
            hit = next((token for token in manifest.forbidden_universal_tokens if token in text), None)
            if hit is not None:
                violations.append(f"{posix(source.relative_to(root))}: {hit}")
    if violations:
        raise OwnershipError("UNIVERSAL source contains Wist-owned semantic or assembly surface: " + "; ".join(sorted(violations)))


def require_artifact_allowlist_empty(manifest: OwnershipManifest) -> None:
    if manifest.legacy_artifact_fields:
        raise OwnershipError("legacyArtifactFieldAllowlist must be empty after data-only Wist artifact migration")


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate deterministic UT/Wist ownership and dependency direction.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        manifest = load_manifest(root)
        require_artifact_allowlist_empty(manifest)
        owners = require_graph_direction(root, manifest)
        require_no_hidden_reverse_identity_edges(root, manifest, owners)
        require_universal_sources_language_neutral(root, manifest, owners)
    except (OSError, ValueError, OwnershipError) as exc:
        print(f"PROJECT_OWNERSHIP=FAIL: {exc}", file=sys.stderr)
        return 1
    counts = {owner: list(owners.values()).count(owner) for owner in ("UNIVERSAL", "WIST_PRODUCT")}
    print(f"PROJECT_OWNERSHIP=PASS UNIVERSAL={counts['UNIVERSAL']} WIST_PRODUCT={counts['WIST_PRODUCT']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
