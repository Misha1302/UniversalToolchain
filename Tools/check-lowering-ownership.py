#!/usr/bin/env python3
"""Keep lowering ownership declarative.

The migration in UT-A046 replaced a thirteen-term `||` chain and 27 `RequireModule(...)` calls that
answered "can this module lower?" with one list in `WistLoweringOwners`. Restoring the chain is a small
edit that compiles, passes the tests, and quietly makes a second source of truth again - one that
drifts from the ownership registry the first time somebody adds a module.

So the shape is forbidden mechanically:

  1. the lowering predicate must delegate to the declarative list rather than compare identifiers;
  2. the declarative list must remain the only place that enumerates lowering owners;
  3. module descriptors must derive the lowering phase from that list, not from the predicate.

Fail closed on anything unparseable, so an unusual construct is a review question and not a silent
loss of coverage.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

LOWERER = Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistSemanticBytecodeLowerer.cs")
OWNERS = Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistLoweringOwners.cs")
DESCRIPTORS = Path("UniversalToolchain/UniversalToolchain.Wist.LanguagePack/WistModuleDescriptors.cs")

# The predicate must be a delegation, not a comparison chain.
DELEGATION = re.compile(r"SupportsModuleContribution\s*\([^)]*\)\s*=>\s*WistLoweringOwners\.OwnsLowering\s*\(")
MAX_PREDICATE_LINES = 3


class OwnershipBoundaryError(RuntimeError):
    pass


def read(root: Path, relative: Path) -> str:
    path = root / relative
    if not path.is_file():
        raise OwnershipBoundaryError(f"expected file is missing: {relative.as_posix()}")
    return path.read_text(encoding="utf-8-sig")


def require_predicate_delegates(root: Path) -> None:
    text = read(root, LOWERER)
    if not DELEGATION.search(text):
        raise OwnershipBoundaryError(
            "WistSemanticBytecodeLowerer.SupportsModuleContribution no longer delegates to "
            "WistLoweringOwners.OwnsLowering. A comparison chain there would reintroduce a second "
            "source of truth beside the declarative list, and the two would drift silently."
        )

    # A `=>` body containing several `||` comparisons is the exact shape that was removed.
    match = re.search(
        r"SupportsModuleContribution\s*\([^)]*\)\s*=>\s*(?P<body>.*?);",
        text,
        re.DOTALL,
    )
    if match is None:
        raise OwnershipBoundaryError("SupportsModuleContribution has no expression body; the gate cannot classify it")
    body = match.group("body")
    if body.count("||") > 0:
        raise OwnershipBoundaryError(
            "SupportsModuleContribution contains an `||` chain. Ownership must be data in "
            "WistLoweringOwners, not a predicate: the chain is what made adding a module a change to "
            "the lowering path."
        )
    if len([line for line in body.splitlines() if line.strip()]) > MAX_PREDICATE_LINES:
        raise OwnershipBoundaryError(
            "SupportsModuleContribution grew beyond a delegation; ownership logic belongs in "
            "WistLoweringOwners, not in the lowering path."
        )


def require_single_enumeration_of_owners(root: Path) -> None:
    """No other production file may express lowering ownership as a comparison chain.

    Mentioning a module identifier is normal - the catalog declares every module, and
    `RequireModule(...)` names the one a construct needs. The shape that must not come back is a
    *predicate*: a run of `id == ... || id == ...` that decides ownership. That is what made adding a
    module a change to the lowering path.
    """
    toolchain = root / "UniversalToolchain"
    if not toolchain.is_dir():
        raise OwnershipBoundaryError("toolchain directory is missing")

    owners_text = read(root, OWNERS)
    declared = re.findall(r"WistContributionIds\.(\w+Module)\b", owners_text)
    if len(declared) < 5:
        raise OwnershipBoundaryError(
            f"WistLoweringOwners declares only {len(declared)} owners; the corpus is implausibly small "
            "and the gate would be near-vacuous"
        )

    offenders: list[str] = []
    for path in sorted(toolchain.rglob("*.cs")):
        parts = path.parts
        if "bin" in parts or "obj" in parts:
            continue
        if path.name in {OWNERS.name, LOWERER.name}:
            continue
        text = path.read_text(encoding="utf-8-sig")
        # A chain is at least three `id ==` comparisons joined by `||` within one statement.
        for statement in re.split(r";", text):
            comparisons = len(re.findall(r"==\s*WistContributionIds\.\w+Module\b", statement))
            if comparisons >= 3 and statement.count("||") >= 2:
                offenders.append(f"{path.relative_to(root).as_posix()} ({comparisons} comparisons)")
                break

    if offenders:
        raise OwnershipBoundaryError(
            "these files express lowering ownership as a comparison chain: "
            + ", ".join(offenders)
            + ". Ownership must have exactly one declarative source in WistLoweringOwners, otherwise "
            "the answers drift silently."
        )


def require_descriptors_derive_from_the_list(root: Path) -> None:
    text = read(root, DESCRIPTORS)
    if "WistLoweringOwners.OwnsLowering" not in text:
        raise OwnershipBoundaryError(
            "WistModuleDescriptors no longer derives the lowering phase from "
            "WistLoweringOwners.OwnsLowering. Deriving it from the lowering predicate instead re-creates "
            "the dependency direction the migration removed."
        )
    if "SupportsModuleContribution" in text:
        raise OwnershipBoundaryError(
            "WistModuleDescriptors references SupportsModuleContribution again; module identity must be "
            "derived from the declarative owner list, not from the lowering path."
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()

    try:
        require_predicate_delegates(root)
        require_single_enumeration_of_owners(root)
        require_descriptors_derive_from_the_list(root)
    except (OwnershipBoundaryError, OSError) as error:
        print(f"lowering-ownership: {error}", file=sys.stderr)
        return 1

    print("lowering-ownership: PASS (single declarative source)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
