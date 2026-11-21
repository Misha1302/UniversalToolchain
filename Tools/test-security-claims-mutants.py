#!/usr/bin/env python3
"""Mutation self-test for Tools/check-security-claims.py.

A doc lint that passes on the real tree proves very little: it could be matching nothing. Each mutant
below is a wording change a real reviewer would wave through, and every one of them must turn the gate
red. The clean control must stay green, so the check cannot be satisfied by refusing everything.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

GATE = Path("Tools/check-security-claims.py")

# (name, file, original, replacement)
MUTANTS: list[tuple[str, str, str, str]] = [
    (
        "affirmative-sandbox-claim",
        "docs/SECURITY.md",
        "Restricted composition, deterministic plans and explicit capability policy are **not** equivalent to a hardened sandbox.",
        "Restricted composition, deterministic plans and explicit capability policy provide a hardened sandbox.",
    ),
    (
        "affirmative-isolation-claim",
        "docs/SECURITY.md",
        "These controls do not interrupt a long-running delegate, bound heap allocation, revoke an already allowed assembly or isolate native calls.",
        "These controls provide process isolation for untrusted packages.",
    ),
    (
        "sandbox-adjective-in-a-feature-list",
        "docs/architecture/external-language-authoring-sdk.md",
        "- hardened hostile-extension sandboxing;",
        "- sandboxed execution for hostile extensions is provided;",
    ),
    (
        "denial-removed-keeps-the-claim",
        "docs/build-dsls/restricted-dsl-security.md",
        "A restricted dialect is a composition boundary. It is useful, but it is not a complete security sandbox.",
        "A restricted dialect is a composition boundary and it is a complete security sandbox.",
    ),
    (
        "runtime-isolation-promise",
        "docs/limitations.md",
        "Hostile input or hostile extension packages require external process/environment isolation, resource controls and a trust model.",
        "Hostile input is handled by the runtime, which provides environment isolation and memory isolation.",
    ),
    # A wrap is not a semantic boundary, but WHICH wrap matters. Breaking the line before the verb still
    # leaves the claim word on the same line as the verb, so a line-judging gate catches it by accident -
    # this exact shape was measured to be caught by the previous version, and a mutant that the old gate
    # already kills proves nothing. The line must break BETWEEN the verb and the claim word:
    #
    #   "... policy provide a"      <- affirmative verb, no claim word on this line
    #   "hardened sandbox."         <- claim word, no verb on this line
    #
    # Neither physical line contains both halves, so a line-judging gate sees a claim noun with no verb
    # and stays silent. Only judging the paragraph finds the claim. This is the shape that was measured
    # to pass the old gate and fail the new one.
    (
        "affirmative-claim-split-between-verb-and-claim-word",
        "docs/SECURITY.md",
        "Restricted composition, deterministic plans and explicit capability policy are **not** equivalent to a hardened sandbox.",
        "Restricted composition, deterministic plans and explicit capability policy provide a\nhardened sandbox.",
    ),
]

# The mirror case: a DENIAL split by a wrap must stay green. This is the false-positive side of the same
# defect, and it is the more dangerous one - a gate that cries wolf on correct prose is a gate that gets
# switched off. Kept in a separate list because a control that must pass cannot live in a list that must
# be rejected.
CONTROLS: list[tuple[str, str, str, str]] = [
    (
        "denial-split-by-a-line-wrap-stays-clean",
        "docs/limitations.md",
        "Hostile input or hostile extension packages require external process/environment isolation, resource controls and a trust model.",
        "The runtime does not\nprovide a security sandbox.",
    ),
]


def run_gate(root: Path) -> tuple[int, str]:
    completed = subprocess.run(
        [sys.executable, str(root / GATE), "--root", str(root)],
        capture_output=True,
        text=True,
        check=False,
    )
    return completed.returncode, (completed.stdout + completed.stderr).strip()


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("."))
    args = parser.parse_args(argv)
    source = args.root.resolve()

    failures: list[str] = []
    with tempfile.TemporaryDirectory() as temporary:
        sandbox = Path(temporary)
        for entry in ("Tools", "docs", "internal-docs"):
            if (source / entry).is_dir():
                shutil.copytree(source / entry, sandbox / entry)
        for name in ("readme.md", "VERIFICATION.md", "RELEASE_NOTES_RU.md"):
            if (source / name).is_file():
                shutil.copy2(source / name, sandbox / name)

        code, output = run_gate(sandbox)
        if code != 0:
            failures.append(f"clean control must pass, got exit {code}: {output[:300]}")
        else:
            print(f"CONTROL=PASS {output}")

        for name, relative, original, replacement in MUTANTS:
            target = sandbox / relative
            if not target.is_file():
                failures.append(f"{name}: fixture file missing: {relative}")
                continue
            text = target.read_text(encoding="utf-8")
            if original not in text:
                failures.append(f"{name}: anchor text not found in {relative}")
                continue

            target.write_text(text.replace(original, replacement, 1), encoding="utf-8")
            code, output = run_gate(sandbox)
            target.write_text(text, encoding="utf-8")

            if code == 0:
                failures.append(f"{name}: mutant SURVIVED the gate ({relative})")
            else:
                print(f"MUTANT={name} REJECTED")

        for name, relative, original, replacement in CONTROLS:
            target = sandbox / relative
            if not target.is_file():
                failures.append(f"{name}: fixture file missing: {relative}")
                continue
            text = target.read_text(encoding="utf-8")
            if original not in text:
                failures.append(f"{name}: anchor text not found in {relative}")
                continue

            target.write_text(text.replace(original, replacement, 1), encoding="utf-8")
            code, output = run_gate(sandbox)
            target.write_text(text, encoding="utf-8")

            if code != 0:
                failures.append(
                    f"{name}: control must stay green but the gate rejected correct prose ({relative}): {output[:200]}"
                )
            else:
                print(f"CONTROL={name} ACCEPTED")

    if failures:
        print("SECURITY-CLAIM-MUTANTS: FAIL", file=sys.stderr)
        for failure in failures:
            print(f"  {failure}", file=sys.stderr)
        return 1
    print(
        f"SECURITY-CLAIM-MUTANTS: PASS ({len(MUTANTS)}/{len(MUTANTS)} rejected, "
        f"{len(CONTROLS)}/{len(CONTROLS)} controls accepted)"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
