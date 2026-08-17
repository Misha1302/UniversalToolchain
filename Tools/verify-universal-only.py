#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import subprocess
import sys
import tempfile
from pathlib import Path


def load_ownership_module(root: Path):
    path = root / "Tools" / "check-project-ownership.py"
    spec = importlib.util.spec_from_file_location("project_ownership", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Cannot load ownership validator from {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def run(*args: str, cwd: Path) -> None:
    print("+", " ".join(args), flush=True)
    subprocess.run(args, cwd=cwd, check=True)


def project_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def is_test_project(path: Path) -> bool:
    text = project_text(path)
    return "Microsoft.NET.Test.Sdk" in text or "<IsTestProject>true</IsTestProject>" in text


def is_packaged_template_consumer(path: Path, root: Path) -> bool:
    relative = path.relative_to(root).as_posix()
    if not relative.startswith("UniversalToolchain/UniversalToolchain.Templates/content/"):
        return False
    # Template content is a generated-consumer fixture: it intentionally validates
    # the public NuGet surface rather than participating in the repository source
    # project graph. Keep it owned/classified, but do not source-build it in the
    # UNIVERSAL-only solution before those packages have been produced.
    return '<PackageReference Include="UniversalToolchain.' in project_text(path)


def main() -> int:
    parser = argparse.ArgumentParser(description="Build and test only projects classified as UNIVERSAL.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()
    ownership = load_ownership_module(root)
    manifest = ownership.load_manifest(root)
    owners = ownership.require_graph_direction(root, manifest)
    ownership.require_no_hidden_reverse_identity_edges(root, manifest, owners)
    ownership.require_universal_sources_language_neutral(root, manifest, owners)

    universal = sorted(path for path, owner in owners.items() if owner == "UNIVERSAL")
    if not universal:
        raise RuntimeError("No UNIVERSAL projects were classified.")

    packaged_consumers = [project for project in universal if is_packaged_template_consumer(project, root)]
    source_projects = [project for project in universal if project not in packaged_consumers]
    tests = [project for project in source_projects if is_test_project(project)]

    # UniversalToolchain/NuGet.config intentionally declares the repository-local
    # packages feed. Canonical build entrypoints provision the directory even when
    # it is empty, so the standalone verifier must preserve the same bootstrap
    # invariant instead of weakening NuGet source validation.
    (root / "UniversalToolchain" / "packages").mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="ut-only-") as tmp:
        workspace = Path(tmp)
        # .NET 10 defaults the sln template to .slnx. This verifier intentionally
        # uses the legacy .sln container below, so make the requested format
        # explicit instead of guessing the extension produced by the SDK.
        run("dotnet", "new", "sln", "-n", "UniversalOnly", "--format", "sln", cwd=workspace)
        solution = workspace / "UniversalOnly.sln"
        for project in source_projects:
            run("dotnet", "sln", str(solution), "add", str(project), cwd=root)
        run("dotnet", "restore", str(solution), "--nologo", cwd=root)
        run("dotnet", "build", str(solution), "-c", "Release", "--no-restore", "--nologo", cwd=root)
        for project in tests:
            run("dotnet", "test", str(project), "-c", "Release", "--no-build", "--nologo", cwd=root)

    print(
        f"UNIVERSAL_ONLY=PASS owned={len(universal)} source_projects={len(source_projects)} "
        f"packaged_consumers={len(packaged_consumers)} tests={len(tests)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
