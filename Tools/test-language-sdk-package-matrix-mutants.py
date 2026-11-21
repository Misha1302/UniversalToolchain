#!/usr/bin/env python3
"""Seeded negative tests for the language-SDK package-matrix gate.

A green run of the gate against a correct package set proves nothing about the gate: every
claim it makes could be an unconditional `return` and the run would still be green. Each
mutant below breaks exactly one claim and asserts that the gate reports it.

The fixtures are synthetic on purpose. The gate runs only after packing, so a harness that
used the real artifacts would be unrunnable in the fast path and would be skipped exactly
when it matters. Synthetic inputs also let a mutant target the special-case branches
(Wist.LanguagePack metadata, Templates configuration) without building nine packages.
"""
from __future__ import annotations

import argparse
import contextlib
import importlib.util
import io
import json
import shutil
import tempfile
import zipfile
from pathlib import Path


def load_checker(root: Path):
    path = root / "Tools" / "check-language-sdk-package-matrix.py"
    spec = importlib.util.spec_from_file_location("language_sdk_package_matrix_checker", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load checker from {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def write_project(path: Path, package_id: str, version: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
        "  <PropertyGroup>\n"
        f"    <PackageId>{package_id}</PackageId>\n"
        f"    <PackageVersion>{version}</PackageVersion>\n"
        "  </PropertyGroup>\n"
        "</Project>\n",
        encoding="utf-8",
    )


def write_package(
    path: Path,
    package_id: str,
    version: str,
    *,
    dependencies: dict[str, str] | None = None,
    extra_entries: dict[str, bytes] | None = None,
    nuspec_override: tuple[str, str] | None = None,
) -> None:
    nuspec_id, nuspec_version = nuspec_override or (package_id, version)
    dependency_xml = ""
    if dependencies:
        items = "".join(
            f'      <dependency id="{key}" version="{value}" />\n' for key, value in sorted(dependencies.items())
        )
        dependency_xml = f"    <dependencies>\n      <group>\n{items}      </group>\n    </dependencies>\n"

    nuspec = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">\n'
        "  <metadata>\n"
        f"    <id>{nuspec_id}</id>\n"
        f"    <version>{nuspec_version}</version>\n"
        "  </metadata>\n"
        f"{dependency_xml}"
        "</package>\n"
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr(f"{package_id}.nuspec", nuspec)
        for name, payload in (extra_entries or {}).items():
            archive.writestr(name, payload)


def feature_manifest(package_id: str, version: str) -> bytes:
    return json.dumps(
        {
            "schemaVersion": 5,
            "canonicalization": "universaltoolchain-json-v1",
            "hashAlgorithm": "sha256",
            "package": {"id": package_id, "version": version},
        }
    ).encode("utf-8")


MATRIX = (
    ("UniversalToolchain.Runtime", "1.0.0"),
    ("UniversalToolchain.Wist", "1.0.0"),
    ("UniversalToolchain.Wist.LanguagePack", "1.0.0"),
    ("UniversalToolchain.Templates", "1.0.0"),
)


def build_fixture(base: Path, *, overrides: dict[str, dict] | None = None, skip: set[str] | None = None) -> tuple[Path, Path]:
    """Create a manifest, its projects, and a matching package directory."""
    overrides = overrides or {}
    skip = skip or set()
    fixture_root = base / "root"
    packages = base / "packages"
    manifest = fixture_root / "eng" / "package-projects.txt"
    manifest.parent.mkdir(parents=True, exist_ok=True)
    lines: list[str] = []

    for package_id, version in MATRIX:
        project_relative = f"projects/{package_id}/{package_id}.csproj"
        write_project(fixture_root / project_relative, package_id, version)
        lines.append(project_relative)

        if package_id in skip:
            continue

        overrides_for_package = overrides.get(package_id, {})
        extra_entries: dict[str, bytes] = {}

        if package_id == "UniversalToolchain.Wist.LanguagePack":
            extra_entries["README.md"] = b"# pack\n"
            extra_entries["content/UniversalToolchain.Wist.LanguagePack.toolchain.feature.json"] = (
                feature_manifest(package_id, version)
            )
            extra_entries.update(overrides_for_package.pop("extra_entries", {}))
            # The embedded descriptor is derived from the manifest unless a mutant rewrites it.
            if "feature_payload" in overrides_for_package:
                extra_entries["content/UniversalToolchain.Wist.LanguagePack.toolchain.feature.json"] = (
                    overrides_for_package.pop("feature_payload")
                )
            if overrides_for_package.pop("drop_readme", False):
                extra_entries.pop("README.md", None)

        if package_id == "UniversalToolchain.Templates":
            extra_entries[".template.config/template.json"] = b"{}"
            if overrides_for_package.pop("drop_template_config", False):
                extra_entries.clear()

        dependencies = {"UniversalToolchain.Runtime": "[1.0.0]"} if package_id != "UniversalToolchain.Runtime" else None
        if "dependencies" in overrides_for_package:
            dependencies = overrides_for_package.pop("dependencies")

        write_package(
            packages / f"{package_id}.{version}.nupkg",
            package_id,
            version,
            dependencies=dependencies,
            extra_entries=extra_entries,
            nuspec_override=overrides_for_package.pop("nuspec_override", None),
        )

    manifest.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return fixture_root, packages


def run_checker(checker, fixture_root: Path, packages: Path) -> list[str]:
    manifest = fixture_root / "eng" / "package-projects.txt"
    return checker.verify_matrix(manifest, packages, fixture_root)


def mutate(base: Path, name: str, apply) -> None:
    fixture_root, packages = build_fixture(base)
    apply(fixture_root, packages)
    checker = load_checker(ROOT)
    with contextlib.redirect_stderr(io.StringIO()):
        errors = run_checker(checker, fixture_root, packages)
    if not errors:
        raise RuntimeError(f"seeded mutant survived package-matrix gate: {name}")
    print(f"SURVIVOR=0 mutant={name}  -> {errors[0][:88]}")


# --- mutants: one per claim the gate makes -------------------------------------------------


def extra_package(fixture_root: Path, packages: Path) -> None:
    source = packages / "UniversalToolchain.Runtime.1.0.0.nupkg"
    shutil.copy(source, packages / "Unexpected.Extra.1.0.0.nupkg")


def missing_package(fixture_root: Path, packages: Path) -> None:
    (packages / "UniversalToolchain.Wist.1.0.0.nupkg").unlink()


def identity_swap(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.1.0.0.nupkg"
    write_package(target, "UniversalToolchain.Wist", "1.0.0", nuspec_override=("UniversalToolchain.Other", "1.0.0"))


def duplicate_nuspec(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.1.0.0.nupkg"
    with zipfile.ZipFile(target, "a", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("second.nuspec", b"<package/>")


def wrong_dependency_version(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.1.0.0.nupkg"
    write_package(target, "UniversalToolchain.Wist", "1.0.0", dependencies={"UniversalToolchain.Runtime": "[9.9.9]"})


def language_pack_without_readme(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.LanguagePack.1.0.0.nupkg"
    with zipfile.ZipFile(target) as archive:
        entries = {info.filename: archive.read(info.filename) for info in archive.infolist()}
    entries.pop("README.md")
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
        for entry_name, payload in entries.items():
            archive.writestr(entry_name, payload)


def language_pack_duplicate_manifest(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.LanguagePack.1.0.0.nupkg"
    with zipfile.ZipFile(target, "a", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("content/second.toolchain.feature.json", feature_manifest("UniversalToolchain.Wist.LanguagePack", "1.0.0"))


def language_pack_schema_downgrade(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.LanguagePack.1.0.0.nupkg"
    payload = json.loads(feature_manifest("UniversalToolchain.Wist.LanguagePack", "1.0.0"))
    payload["schemaVersion"] = 4
    rewrite_entry(target, "content/UniversalToolchain.Wist.LanguagePack.toolchain.feature.json", json.dumps(payload).encode())


def language_pack_wrong_canonicalization(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.LanguagePack.1.0.0.nupkg"
    payload = json.loads(feature_manifest("UniversalToolchain.Wist.LanguagePack", "1.0.0"))
    payload["canonicalization"] = "other"
    rewrite_entry(target, "content/UniversalToolchain.Wist.LanguagePack.toolchain.feature.json", json.dumps(payload).encode())


def language_pack_wrong_hash_algorithm(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.LanguagePack.1.0.0.nupkg"
    payload = json.loads(feature_manifest("UniversalToolchain.Wist.LanguagePack", "1.0.0"))
    payload["hashAlgorithm"] = "sha1"
    rewrite_entry(target, "content/UniversalToolchain.Wist.LanguagePack.toolchain.feature.json", json.dumps(payload).encode())


def language_pack_identity_mismatch(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Wist.LanguagePack.1.0.0.nupkg"
    payload = json.loads(feature_manifest("UniversalToolchain.Wist.LanguagePack", "1.0.0"))
    payload["package"]["version"] = "9.9.9"
    rewrite_entry(target, "content/UniversalToolchain.Wist.LanguagePack.toolchain.feature.json", json.dumps(payload).encode())


def templates_without_config(fixture_root: Path, packages: Path) -> None:
    target = packages / "UniversalToolchain.Templates.1.0.0.nupkg"
    with zipfile.ZipFile(target) as archive:
        entries = {info.filename: archive.read(info.filename) for info in archive.infolist()}
    entries.pop(".template.config/template.json")
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
        for entry_name, payload in entries.items():
            archive.writestr(entry_name, payload)


def duplicate_manifest_id(fixture_root: Path, packages: Path) -> None:
    manifest = fixture_root / "eng" / "package-projects.txt"
    extra_project = fixture_root / "projects" / "duplicate" / "Duplicate.csproj"
    write_project(extra_project, "UniversalToolchain.Runtime", "1.0.0")
    manifest.write_text(
        manifest.read_text(encoding="utf-8") + "projects/duplicate/Duplicate.csproj\n",
        encoding="utf-8",
    )


def rewrite_entry(path: Path, entry_name: str, payload: bytes) -> None:
    with zipfile.ZipFile(path) as archive:
        entries = {info.filename: archive.read(info.filename) for info in archive.infolist()}
    entries[entry_name] = payload
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, data in entries.items():
            archive.writestr(name, data)


MUTANTS = {
    "extra-package": extra_package,
    "missing-package": missing_package,
    "identity-swap": identity_swap,
    "duplicate-nuspec": duplicate_nuspec,
    "wrong-dependency-version": wrong_dependency_version,
    "language-pack-without-readme": language_pack_without_readme,
    "language-pack-duplicate-manifest": language_pack_duplicate_manifest,
    "language-pack-schema-downgrade": language_pack_schema_downgrade,
    "language-pack-wrong-canonicalization": language_pack_wrong_canonicalization,
    "language-pack-wrong-hash-algorithm": language_pack_wrong_hash_algorithm,
    "language-pack-identity-mismatch": language_pack_identity_mismatch,
    "templates-without-config": templates_without_config,
    "duplicate-manifest-id": duplicate_manifest_id,
}

DEFAULT_ROOT = Path(__file__).resolve().parents[1]
ROOT = DEFAULT_ROOT


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=DEFAULT_ROOT)
    args = parser.parse_args()

    global ROOT
    ROOT = args.root.resolve()

    # Guard: the fixture itself must pass, otherwise every mutant would be "killed" by a
    # defect in the harness rather than by the gate.
    with tempfile.TemporaryDirectory(prefix="package-matrix-clean-") as tmp:
        fixture_root, packages = build_fixture(Path(tmp))
        checker = load_checker(ROOT)
        with contextlib.redirect_stderr(io.StringIO()):
            errors = run_checker(checker, fixture_root, packages)
        if errors:
            raise RuntimeError(f"clean fixture must pass the gate, got: {errors}")

    for name, apply in MUTANTS.items():
        with tempfile.TemporaryDirectory(prefix=f"package-matrix-{name}-") as tmp:
            mutate(Path(tmp), name, apply)

    print(f"All {len(MUTANTS)} package-matrix mutants killed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
