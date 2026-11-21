#!/usr/bin/env python3
"""Enforce ADR-008: modules are descriptive, never a planner concept.

The descriptor exists so lowering dispatch and diagnostics can name the mechanism that owns a
contribution. The moment the planner reads it, "module" becomes a second selection layer beside
features and contributions - the exact shape ADR-008 rejects. That change compiles and produces
plausible-looking plans, so review alone is not a reliable guard; the dependency direction is
asserted mechanically here and in `ModuleDescriptorNotInPlannerTests`.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


class BoundaryError(RuntimeError):
    pass


PLANNER_DIRECTORY = Path("UniversalToolchain/UniversalToolchain.LanguageSdk")
REGISTRY = Path("UniversalToolchain/UniversalToolchain.FeatureSdk/LanguageModuleRegistry.cs")
DESCRIPTOR = Path("UniversalToolchain/UniversalToolchain.FeatureSdk/LanguageModuleDescriptor.cs")

# The registry may answer "who owns this phase of this contribution". It may not answer "what should
# be enabled", which is the planner's question.
ALLOWED_REGISTRY_METHODS = {"Add", "AddRange", "TryGetOwner"}


def production_cs_files(directory: Path) -> list[Path]:
    if not directory.is_dir():
        raise BoundaryError(f"directory does not exist: {directory}")
    return sorted(
        path for path in directory.rglob("*.cs")
        if "bin" not in path.parts and "obj" not in path.parts
    )


def require_planner_ignores_modules(root: Path) -> None:
    offenders = [
        path.relative_to(root).as_posix()
        for path in production_cs_files(root / PLANNER_DIRECTORY)
        if "LanguageModuleDescriptor" in path.read_text(encoding="utf-8-sig")
    ]
    if offenders:
        raise BoundaryError(
            "LanguageSdk is the single planner and must not reference LanguageModuleDescriptor "
            f"(ADR-008). Offenders: {', '.join(offenders)}"
        )


def require_registry_has_no_selection_surface(root: Path) -> None:
    path = root / REGISTRY
    text = path.read_text(encoding="utf-8-sig")
    declared = {
        match.group(1)
        for match in re.finditer(
            r"public\s+(?:sealed\s+)?[\w<>,\[\]?.\s]+?\s+(\w+)\s*\(", text
        )
    }
    unexpected = sorted(declared - ALLOWED_REGISTRY_METHODS)
    if unexpected:
        raise BoundaryError(
            "LanguageModuleRegistry exposes selection-shaped public methods "
            f"{unexpected}; only {sorted(ALLOWED_REGISTRY_METHODS)} are permitted, because a registry "
            "that can decide what to enable is a second planner."
        )
    missing = sorted(ALLOWED_REGISTRY_METHODS - declared)
    if missing:
        raise BoundaryError(f"LanguageModuleRegistry is missing expected public methods: {missing}")


def require_descriptor_states_it_is_not_a_planner_node(root: Path) -> None:
    text = (root / DESCRIPTOR).read_text(encoding="utf-8-sig")
    if "not</em> a planner" not in text:
        raise BoundaryError(
            "LanguageModuleDescriptor must keep its explicit non-planner statement; without it the "
            "next reader reasonably assumes modules participate in resolution."
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        require_planner_ignores_modules(root)
        require_registry_has_no_selection_surface(root)
        require_descriptor_states_it_is_not_a_planner_node(root)
    except (BoundaryError, OSError) as error:
        print(f"module-descriptor-boundary: {error}", file=sys.stderr)
        return 1
    print("module-descriptor-boundary: PASS (ADR-008)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
