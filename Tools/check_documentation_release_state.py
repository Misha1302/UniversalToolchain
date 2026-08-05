#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

DEFAULT_STATE = Path("eng/documentation-release-state.json")
PUBLISHED_MARKER_BEGIN = "<!-- wist-published-install:begin -->"
PUBLISHED_MARKER_END = "<!-- wist-published-install:end -->"
SOURCE_MARKER_BEGIN = "<!-- wist-source-candidate:begin -->"
SOURCE_MARKER_END = "<!-- wist-source-candidate:end -->"


class ReleaseStateError(ValueError):
    pass


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def child_text(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if local_name(element.tag) == name:
            value = (element.text or "").strip()
            if value:
                return value
    return None


def require_string(data: dict[str, object], key: str) -> str:
    value = data.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ReleaseStateError(f"release state field {key!r} must be a non-empty string")
    return value.strip()


def require_string_list(data: dict[str, object], key: str) -> list[str]:
    value = data.get(key)
    if not isinstance(value, list) or not value or not all(isinstance(item, str) and item.strip() for item in value):
        raise ReleaseStateError(f"release state field {key!r} must be a non-empty string list")
    return [item.strip() for item in value]


def load_state(path: Path) -> dict[str, object]:
    if not path.is_file():
        raise ReleaseStateError(f"release state file does not exist: {path}")
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ReleaseStateError(f"invalid release state JSON: {exc}") from exc
    if not isinstance(data, dict):
        raise ReleaseStateError("release state root must be an object")
    if data.get("schemaVersion") != 1:
        raise ReleaseStateError(f"unsupported release state schemaVersion: {data.get('schemaVersion')!r}")
    return data


def read_project_identity(project: Path) -> tuple[str, str, str]:
    if not project.is_file():
        raise ReleaseStateError(f"package project does not exist: {project}")
    try:
        root = ET.parse(project).getroot()
    except ET.ParseError as exc:
        raise ReleaseStateError(f"invalid project XML {project}: {exc}") from exc
    package_id = child_text(root, "PackageId") or project.stem
    version = child_text(root, "PackageVersion") or child_text(root, "Version")
    target_framework = child_text(root, "TargetFramework")
    if not version or not target_framework:
        raise ReleaseStateError(f"project identity is incomplete: {project}")
    return package_id, version, target_framework


def marked_block(text: str, begin: str, end: str, document: Path) -> str:
    if text.count(begin) != 1 or text.count(end) != 1:
        raise ReleaseStateError(
            f"{document}: expected exactly one marker pair {begin!r} / {end!r}"
        )
    return text.split(begin, 1)[1].split(end, 1)[0]


def compact_command_text(text: str) -> str:
    return re.sub(r"\\\s*\n\s*", " ", text)


def validate_install_block(
    document: Path,
    block: str,
    package_id: str,
    version: str,
    *,
    require_nuget_source: bool,
) -> None:
    compact = compact_command_text(block)
    command = re.compile(
        rf"dotnet\s+add\s+package\s+{re.escape(package_id)}\b(?:(?!dotnet\s+add\s+package).)*?"
        rf"--version\s+{re.escape(version)}\b",
        re.DOTALL,
    )
    if not command.search(compact):
        raise ReleaseStateError(
            f"{document}: install block does not contain {package_id} {version}"
        )
    has_nuget_source = "https://api.nuget.org/v3/index.json" in compact
    if require_nuget_source and not has_nuget_source:
        raise ReleaseStateError(f"{document}: published install must pin the NuGet.org v3 feed")
    if not require_nuget_source and "--source" not in compact:
        raise ReleaseStateError(f"{document}: source-candidate install must name its local feed")


def validate_build_commands(document: Path, text: str) -> None:
    for match in re.finditer(r"(?m)^\s*(\./build\.sh[^\n]*)$", text):
        command = match.group(1)
        if "--skip-pack" in command:
            continue
        if "--baseline-source-archive" in command and "--previous-package-bundle" in command:
            continue
        raise ReleaseStateError(
            f"{document}: build command would enter the baseline-bearing package gate without its inputs: {command}"
        )


def validate_root_readme_links(root: Path) -> None:
    readme = root / "readme.md"
    if not readme.is_file():
        raise ReleaseStateError("readme.md does not exist")
    text = readme.read_text(encoding="utf-8")
    for raw in re.findall(r"!?\[[^\]]*\]\(([^)]+)\)", text):
        target = raw.strip()
        if target.startswith("<") and target.endswith(">"):
            target = target[1:-1]
        target = re.split(r"\s+['\"]", target, maxsplit=1)[0]
        target = target.split("#", 1)[0]
        if not target or target.startswith(("http://", "https://", "mailto:", "tel:", "data:")):
            continue
        candidate = (root / target).resolve()
        try:
            candidate.relative_to(root)
        except ValueError as exc:
            raise ReleaseStateError(f"readme.md: local link escapes repository: {raw}") from exc
        if candidate.is_dir():
            candidate = candidate / "index.md"
        elif not candidate.exists() and candidate.suffix == "":
            markdown = Path(str(candidate) + ".md")
            candidate = markdown if markdown.exists() else candidate / "index.md"
        if not candidate.exists():
            raise ReleaseStateError(f"readme.md: missing local link target: {raw}")


def validate(root: Path, state_path: Path) -> dict[str, str]:
    state = load_state(state_path)
    package_id = require_string(state, "packageId")
    source_version = require_string(state, "sourceVersion")
    published_version = require_string(state, "publishedVersion")
    target_framework = require_string(state, "targetFramework")
    project_path = require_string(state, "project")
    stability_path = require_string(state, "stabilityDocument")
    published_documents = require_string_list(state, "publishedInstallDocuments")
    source_documents = require_string_list(state, "sourceCandidateDocuments")
    build_documents = require_string_list(state, "sourceBuildDocuments")
    stability_link_documents = require_string_list(state, "stabilityLinkDocuments")
    workflow_path = require_string(state, "publishedSmokeWorkflow")

    project = root / project_path
    actual_id, actual_source_version, actual_target_framework = read_project_identity(project)
    if actual_id != package_id:
        raise ReleaseStateError(
            f"release state packageId {package_id} does not match project PackageId {actual_id}"
        )
    if actual_source_version != source_version:
        raise ReleaseStateError(
            f"release state sourceVersion {source_version} does not match project version {actual_source_version}"
        )
    if actual_target_framework != target_framework:
        raise ReleaseStateError(
            f"release state targetFramework {target_framework} does not match project target {actual_target_framework}"
        )

    stability = root / stability_path
    if not stability.is_file():
        raise ReleaseStateError(f"active stability document does not exist: {stability_path}")
    stability_text = stability.read_text(encoding="utf-8")
    if source_version not in stability_text:
        raise ReleaseStateError(
            f"{stability_path}: active stability document does not identify source version {source_version}"
        )

    for relative in published_documents:
        document = root / relative
        if not document.is_file():
            raise ReleaseStateError(f"published-install document does not exist: {relative}")
        text = document.read_text(encoding="utf-8")
        block = marked_block(text, PUBLISHED_MARKER_BEGIN, PUBLISHED_MARKER_END, document)
        validate_install_block(
            document,
            block,
            package_id,
            published_version,
            require_nuget_source=True,
        )
        if source_version != published_version and source_version in block:
            raise ReleaseStateError(
                f"{relative}: published install block contains unpublished source version {source_version}"
            )

    for relative in source_documents:
        document = root / relative
        if not document.is_file():
            raise ReleaseStateError(f"source-candidate document does not exist: {relative}")
        text = document.read_text(encoding="utf-8")
        block = marked_block(text, SOURCE_MARKER_BEGIN, SOURCE_MARKER_END, document)
        if source_version not in block:
            raise ReleaseStateError(
                f"{relative}: source-candidate block does not identify source version {source_version}"
            )
        lowered = block.lower()
        if published_version != source_version and not any(
            phrase in lowered for phrase in ("not published", "unpublished", "not on nuget.org")
        ):
            raise ReleaseStateError(
                f"{relative}: unpublished source candidate must be labeled as not published"
            )

    for relative in build_documents:
        document = root / relative
        if not document.is_file():
            raise ReleaseStateError(f"source-build document does not exist: {relative}")
        validate_build_commands(document, document.read_text(encoding="utf-8"))

    stability_link = "/" + stability_path.removeprefix("docs/").removesuffix(".md")
    for relative in stability_link_documents:
        document = root / relative
        if not document.is_file():
            raise ReleaseStateError(f"stability-link document does not exist: {relative}")
        text = document.read_text(encoding="utf-8")
        if stability_link not in text and stability_path not in text:
            raise ReleaseStateError(
                f"{relative}: active stability link {stability_link} is missing"
            )

    validate_root_readme_links(root)

    workflow = root / workflow_path
    if not workflow.is_file():
        raise ReleaseStateError(f"published-package smoke workflow does not exist: {workflow_path}")
    workflow_text = workflow.read_text(encoding="utf-8")
    if "PUBLISHED_WIST_VERSION:" in workflow_text:
        raise ReleaseStateError(
            f"{workflow_path}: published version must come from {state_path.relative_to(root)}, not a workflow literal"
        )
    if "--print-published-version" not in workflow_text:
        raise ReleaseStateError(
            f"{workflow_path}: workflow does not resolve the published version through the release-state checker"
        )

    return {
        "packageId": package_id,
        "sourceVersion": source_version,
        "publishedVersion": published_version,
        "targetFramework": target_framework,
        "stabilityDocument": stability_path,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate Wist documentation release state and first-contact commands.")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--state", type=Path, default=DEFAULT_STATE)
    parser.add_argument("--print-published-version", action="store_true")
    parser.add_argument("--print-source-version", action="store_true")
    args = parser.parse_args(argv)

    root = args.root.resolve()
    state_path = args.state.resolve() if args.state.is_absolute() else (root / args.state).resolve()
    try:
        result = validate(root, state_path)
    except (OSError, ReleaseStateError) as exc:
        print(f"documentation-release-state: ERROR: {exc}", file=sys.stderr)
        return 1

    if args.print_published_version:
        print(result["publishedVersion"])
    elif args.print_source_version:
        print(result["sourceVersion"])
    else:
        print(
            "documentation-release-state: PASS "
            f"published={result['publishedVersion']} source={result['sourceVersion']} "
            f"target={result['targetFramework']}"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
