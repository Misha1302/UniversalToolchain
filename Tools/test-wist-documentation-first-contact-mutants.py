#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import tempfile
from pathlib import Path

EXCLUDED = shutil.ignore_patterns(
    ".git", "artifacts", "bin", "obj", "node_modules", "packages", ".cache"
)


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["python3", str(checker), "--root", str(root), "--skip-compile"],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def require_killed(source_root: Path, name: str, mutate) -> None:
    with tempfile.TemporaryDirectory(prefix=f"wist-doc-first-contact-{name}-") as temp_name:
        mutant_root = Path(temp_name) / "repo"
        shutil.copytree(source_root, mutant_root, ignore=EXCLUDED)
        mutate(mutant_root)
        completed = run(
            mutant_root / "Tools/check-wist-documentation-first-contact.py",
            mutant_root,
        )
        if completed.returncode == 0:
            raise RuntimeError(f"{name} mutant survived:\n{completed.stdout}")
        print(f"SURVIVOR=0 mutant={name}")


def replace(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"mutation precondition missing in {path}: {old}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()
    checker = root / "Tools/check-wist-documentation-first-contact.py"
    state_path = root / "eng/documentation-release-state.json"
    state = json.loads(state_path.read_text(encoding="utf-8"))
    source_version = state["sourceVersion"]
    package_readme = root / state["packageReadmeDocuments"][0]
    package_readme_identity = (
        f"This README describes `UniversalToolchain.Wist` `{source_version}`."
    )

    positive = run(checker, root)
    if positive.returncode != 0:
        raise RuntimeError("positive first-contact check failed:\n" + positive.stdout)
    print("positive-control=1 wist-documentation-first-contact")

    require_killed(
        root,
        "invalid-validation-result-member",
        lambda mutant: replace(
            mutant / "readme.md",
            "validation.Diagnostics.Select(diagnostic => diagnostic.Message)",
            "validation.Message",
        ),
    )
    require_killed(
        root,
        "repository-local-package-feed",
        lambda mutant: replace(
            mutant / package_readme.relative_to(root),
            f"dotnet add package UniversalToolchain.Wist --version {source_version}",
            (
                f"dotnet add package UniversalToolchain.Wist --version {source_version} "
                "--source ./artifacts/packages"
            ),
        ),
    )

    def misregister_package_readme(mutant: Path) -> None:
        mutant_state_path = mutant / "eng/documentation-release-state.json"
        mutant_state = json.loads(mutant_state_path.read_text(encoding="utf-8"))
        mutant_state["packageReadmeDocuments"] = ["readme.md"]
        mutant_state_path.write_text(json.dumps(mutant_state, indent=2) + "\n", encoding="utf-8")

    require_killed(root, "packed-readme-unregistered", misregister_package_readme)

    def overlap_package_readme_with_source_contract(mutant: Path) -> None:
        mutant_state_path = mutant / "eng/documentation-release-state.json"
        mutant_state = json.loads(mutant_state_path.read_text(encoding="utf-8"))
        package_path = mutant_state["packageReadmeDocuments"][0]
        mutant_state["sourceCandidateDocuments"].append(package_path)
        mutant_state_path.write_text(json.dumps(mutant_state, indent=2) + "\n", encoding="utf-8")

    require_killed(
        root,
        "package-readme-source-contract-overlap",
        overlap_package_readme_with_source_contract,
    )
    require_killed(
        root,
        "package-readme-source-marker",
        lambda mutant: replace(
            mutant / package_readme.relative_to(root),
            package_readme_identity,
            (
                "<!-- wist-source-candidate:begin -->\n"
                f"{package_readme_identity}\n"
                "<!-- wist-source-candidate:end -->"
            ),
        ),
    )
    require_killed(
        root,
        "temporal-unpublished-claim",
        lambda mutant: replace(
            mutant / package_readme.relative_to(root),
            package_readme_identity,
            "This candidate is not published on NuGet.org.",
        ),
    )
    require_killed(
        root,
        "temporal-published-claim",
        lambda mutant: replace(
            mutant / package_readme.relative_to(root),
            package_readme_identity,
            "This version is published on NuGet.org.",
        ),
    )
    require_killed(
        root,
        "stale-runtime-assembly-count",
        lambda mutant: replace(
            mutant / package_readme.relative_to(root),
            "Assemblies under `lib/net10.0` form the runtime implementation closure",
            "The 64 assemblies under `lib/net10.0` form the runtime implementation closure",
        ),
    )
    require_killed(
        root,
        "missing-quickstart-marker",
        lambda mutant: replace(
            mutant / "readme.md",
            "<!-- wist-source-quickstart-csharp:begin -->",
            "<!-- removed-wist-source-quickstart-csharp:begin -->",
        ),
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
