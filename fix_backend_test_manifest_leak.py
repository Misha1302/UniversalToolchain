#!/usr/bin/env python3
"""
Fix test-runtime pollution introduced by ThirdBackendRuntimeComponentContractTests.

The third-backend contract tests must build fake RuntimeComponentManifestEntry objects
manually. They must NOT export a fake backend from the test assembly with
[DialectRuntimeExport], otherwise the normal runtime manifest emitter discovers it and the
canonical Wist runtime catalog starts seeing a third backend during unrelated tests.

Run from the Wist2 repository root:

    python3 fix_backend_test_manifest_leak.py

Then run:

    dotnet test UniversalToolchain/Tests/Tests.csproj --filter CanonicalWistRuntimeFlow_ShouldRemainStable_UnderMixedLoad
    dotnet test UniversalToolchain
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


TEST_PATH = Path(
    "UniversalToolchain/UniversalToolchain.Dialects.Tests/RuntimeLoading/ThirdBackendRuntimeComponentContractTests.cs"
)

ATTRIBUTE_BLOCK = '''    [DialectRuntimeExport("Backend", ThirdBackendId)]
    [DialectRuntimeAlias(ThirdBackendAlias)]
    [DialectRuntimeAlias(ThirdBackendSecondAlias)]
    [DialectBackendRegistrarType(typeof(ThirdBackendRegistrar))]
    private sealed class ThirdBackendDeclaration : DialectBackendDeclaration
'''

REPLACEMENT_BLOCK = '''    // Intentionally no DialectRuntimeExport/DialectRuntimeAlias/DialectBackendRegistrarType attributes here.
    // The test creates RuntimeComponentManifestEntry manually. Exporting this fake backend from
    // the test assembly would make the normal manifest emitter publish it into the shared runtime
    // catalog and would contaminate unrelated canonical Wist runtime tests.
    private sealed class ThirdBackendDeclaration : DialectBackendDeclaration
'''


def main() -> int:
    parser = argparse.ArgumentParser(description="Remove fake third backend runtime export attributes from tests.")
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--allow-non-master", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    repo_root = args.repo_root.resolve()
    validate_repo_root(repo_root)
    validate_branch(repo_root, args.allow_non_master)

    path = repo_root / TEST_PATH
    text = read_required(path)

    if ATTRIBUTE_BLOCK not in text:
        if "Intentionally no DialectRuntimeExport" in text:
            print("No changes needed: fake backend export attributes are already removed.")
            return 0

        fail(
            "Could not find the expected fake backend attribute block. "
            "Open ThirdBackendRuntimeComponentContractTests.cs and remove runtime export attributes manually."
        )

    updated = text.replace(ATTRIBUTE_BLOCK, REPLACEMENT_BLOCK)

    if args.dry_run:
        print(f"Would update {TEST_PATH}")
        return 0

    path.write_text(updated, encoding="utf-8")
    print(f"Updated {TEST_PATH}")
    print()
    print("Now clean stale emitted manifests before retesting:")
    print("  find UniversalToolchain -type d \\( -name bin -o -name obj \\) -prune -exec rm -rf {} +")
    print("  dotnet restore UniversalToolchain")
    print("  dotnet test UniversalToolchain/Tests/Tests.csproj --filter CanonicalWistRuntimeFlow_ShouldRemainStable_UnderMixedLoad")
    print("  dotnet test UniversalToolchain")
    return 0


def validate_repo_root(repo_root: Path) -> None:
    if not (repo_root / "UniversalToolchain").exists():
        fail(f"This does not look like the Wist2 root: {repo_root}")

    if not (repo_root / TEST_PATH).exists():
        fail(f"Expected test file is missing: {TEST_PATH}")


def validate_branch(repo_root: Path, allow_non_master: bool) -> None:
    if allow_non_master:
        return

    try:
        result = subprocess.run(
            ["git", "branch", "--show-current"],
            cwd=repo_root,
            check=True,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except (OSError, subprocess.CalledProcessError) as ex:
        fail(f"Could not determine current git branch. Use --allow-non-master to bypass. Error: {ex}")

    branch = result.stdout.strip()
    if branch != "master":
        fail(f"Refusing to patch branch '{branch}'. Checkout master or pass --allow-non-master.")


def read_required(path: Path) -> str:
    if not path.exists():
        fail(f"Missing file: {path}")

    return path.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


if __name__ == "__main__":
    raise SystemExit(main())
