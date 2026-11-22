#!/usr/bin/env python3
"""Check active SDK onboarding versions against packable project identities."""
from __future__ import annotations

from pathlib import Path
import re
import xml.etree.ElementTree as ET

from check_documentation_release_state import child_text


def package_versions(root: Path) -> dict[str, str]:
    versions: dict[str, str] = {}
    for project in sorted((root / "UniversalToolchain").glob("*/*.csproj")):
        tree = ET.parse(project).getroot()
        package = child_text(tree, "PackageId")
        version = child_text(tree, "PackageVersion") or child_text(tree, "Version")
        if package and version:
            versions[package] = version
    return versions


def check_references(text: str, versions: dict[str, str], document: str) -> list[str]:
    errors: list[str] = []
    for match in re.finditer(r"<PackageReference\b[^>]*(?:/>|>.*?</PackageReference>)", text, re.DOTALL):
        try:
            reference = ET.fromstring(match.group(0))
        except ET.ParseError as exc:
            errors.append(f"{document}: invalid PackageReference XML: {exc}")
            continue
        package = reference.get("Include", "")
        if not package.startswith("UniversalToolchain."):
            continue
        version = reference.get("Version") or child_text(reference, "Version")
        if package not in versions or version != versions[package]:
            errors.append(f"{document}: {package} references {version}; source project declares {versions.get(package, 'unknown package')}")
    for version in re.findall(r"UniversalToolchain\.Templates\.([0-9][A-Za-z0-9.+-]*)\.nupkg", text):
        expected = versions.get("UniversalToolchain.Templates")
        if version != expected:
            errors.append(f"{document}: template filename uses {version}; source project declares {expected}")
    return errors


def check(root: Path) -> list[str]:
    errors: list[str] = []
    try:
        versions = package_versions(root)
        for relative in (
            "docs/language-authoring/quickstart.md",
            "docs/language-authoring/testing-and-templates.md",
            "UniversalToolchain/UniversalToolchain.Templates/content/ut-language/TemplateLanguage.csproj",
        ):
            errors.extend(check_references((root / relative).read_text(encoding="utf-8"), versions, relative))
    except (OSError, ET.ParseError) as exc:
        errors.append(f"onboarding package contract cannot be checked: {exc}")
    return errors


if __name__ == "__main__":
    problems = check(Path(__file__).resolve().parents[1])
    print("\n".join(problems) if problems else "SDK onboarding versions: PASS")
    raise SystemExit(1 if problems else 0)
