#!/usr/bin/env python3
"""Verify that the committed package lock files cover the projects that need them.

Restore resolves NuGet packages freshly on every run unless a lock file pins the result.
`RestoreLockedMode` protects a lock that exists, but it does *not* require one to exist: a
project with no `packages.lock.json` restores happily and writes a fresh lock, so the build
stays green while resolution drifts. Coverage therefore has to be checked separately, which
is what this gate does.

Three failure modes are covered:

1. a project with direct `PackageReference` entries has no lock file;
2. a committed lock file is empty, which means it pins nothing while looking authoritative;
3. a lock file is committed where it must not be, notably inside the `dotnet new` template
   content, which is packed verbatim into the template package.
"""
from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


SKIP_PARTS = {"bin", "obj", "node_modules"}
# The template payload is packed into the `UniversalToolchain.Templates` package
# (`Content Include="content\**\*" ... NoDefaultExcludes=true`). A lock file there would be
# shipped to every template consumer's project directory, where it would fail to match their
# own graph. The template is materialised and restored by the smoke test, not from here.
TEMPLATE_CONTENT = Path("UniversalToolchain/UniversalToolchain.Templates/content")


class LockError(ValueError):
    pass


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def candidate_projects(root: Path) -> list[Path]:
    projects = []
    for path in root.rglob("*.csproj"):
        if SKIP_PARTS.intersection(path.parts):
            continue
        if path.relative_to(root).parts[0] == "artifacts":
            continue
        projects.append(path)
    return sorted(projects)


def has_direct_package_reference(project: Path) -> bool:
    try:
        document = ET.parse(project).getroot()
    except ET.ParseError as exc:
        raise LockError(f"invalid project XML {project}: {exc}") from exc
    # Only PackageReference is a NuGet dependency. ProjectReference is a project edge and does
    # not resolve a package, so it does not require a lock file of its own.
    return any(local_name(element.tag) == "PackageReference" for element in document.iter())


def lock_files(root: Path) -> list[Path]:
    files = []
    for path in root.rglob("packages.lock.json"):
        if SKIP_PARTS.intersection(path.parts):
            continue
        if path.relative_to(root).parts[0] == "artifacts":
            continue
        files.append(path)
    return sorted(files)


def pins_anything(lock_path: Path) -> bool:
    """A lock pins something when at least one entry carries a resolved package content hash.

    Locks that describe only project-to-project edges are not useful: the graph already records
    those, and holding a lock for them makes every added ProjectReference fail under locked mode
    until all of them are regenerated in bulk.
    """
    data = json.loads(lock_path.read_text(encoding="utf-8"))
    for section in data.get("dependencies", {}).values():
        for entry in section.values():
            if isinstance(entry, dict) and "contentHash" in entry:
                return True
    return False


def verify(root: Path) -> list[str]:
    problems: list[str] = []

    for project in candidate_projects(root):
        relative = project.relative_to(root)
        if TEMPLATE_CONTENT in relative.parents:
            if (project.parent / "packages.lock.json").exists():
                problems.append(
                    f"{relative.parent}/packages.lock.json must not be committed: the template "
                    "content is packed verbatim and would ship a lock unrelated to the consumer"
                )
            continue
        if has_direct_package_reference(project) and not (project.parent / "packages.lock.json").exists():
            problems.append(
                f"{relative} resolves NuGet packages but has no packages.lock.json; "
                "restore would resolve transitive versions freely"
            )

    for lock_path in lock_files(root):
        relative = lock_path.relative_to(root)
        if TEMPLATE_CONTENT in relative.parents:
            continue
        project = lock_path.parent / f"{lock_path.parent.name}.csproj"
        siblings = list(lock_path.parent.glob("*.csproj"))
        if not pins_anything(lock_path):
            problems.append(
                f"{relative} pins no packages; delete it so it does not gate project references"
            )
        elif not siblings:
            problems.append(f"{relative} has no project beside it")

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify committed NuGet lock file coverage.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()

    try:
        problems = verify(args.root.resolve())
    except (LockError, OSError, ValueError) as exc:
        print(f"PACKAGE_LOCKS=FAIL: {exc}", file=sys.stderr)
        return 1

    if problems:
        for problem in problems:
            print(f"PACKAGE_LOCKS=FAIL: {problem}", file=sys.stderr)
        return 1

    print("PACKAGE_LOCKS=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
